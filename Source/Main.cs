// This is free and unencumbered software released into the public domain.
// 
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
// 
// In jurisdictions that recognize copyright laws, the author or authors
// of this software dedicate any and all copyright interest in the
// software to the public domain. We make this dedication for the benefit
// of the public at large and to the detriment of our heirs and
// successors. We intend this dedication to be an overt act of
// relinquishment in perpetuity of all present and future rights to this
// software under copyright law.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
// OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
// 
// For more information, please refer to <http://unlicense.org/>

using System;
using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Plugins;

namespace TreeDeduplicator {
    public class TreeDeduplicator : IUserMod {
        public string Name {
            get { return "The Amazing Tree Deduplicator"; }
        }
        public string Description {
            get { return "Gets rid of overlapping trees."; }
        }
    }

    public static class Util {
        public static void Log (string msg) {
            DebugOutputPanel.Show();
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message,
                "[TreeDeduplicator] " + msg);
        }
    }

    public class LoadingExtension : LoadingExtensionBase {
        public override void OnLevelLoaded (LoadMode mode) {
            // See https://gist.github.com/reima/9ba51c69f65ae2da7909
            var ui = UIView.GetAView();
            var button = (UIButton) ui.AddUIComponent (typeof(UIButton));
            // Set the button's text and size.
            button.text = "Deduplicate Trees";
            button.width = 200;
            button.height = 30;
            // Style the button to look like a menu button.
            button.normalBgSprite = "ButtonMenu";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.textColor = new Color32(255, 255, 255, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            button.focusedTextColor = new Color32(255, 255, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);
            button.playAudioEvents = true;
            // Place the button.
            // FIXME: Might overlap some other mods' buttons. (Traffic++?)
            button.transformPosition = new Vector3(-1.65f, 0.97f);
            // Set the callback for clicks.
            button.eventClick += OnButtonClick;
        }
        private struct TreeEntry {
            public uint id;
            public short x;
            public short z;
        }
        private void OnButtonClick (UIComponent uiComponent,
            UIMouseEventParameter mouseEventParam)
        {
            // Get the global TreeManager singleton.
            if (!Singleton<TreeManager>.exists) {
                Util.Log("TreeManager doesn't exist for some reason.");
                return;
            }
            var tm = Singleton<TreeManager>.instance;
            Util.Log("TreeManager count: " + tm.m_treeCount.ToString());
/*
    So here's how trees in Cities: Skylines are stored.
    First, take a look at these three bare-bones "documentation" pages:
    - https://github.com/cities-skylines/Assembly-CSharp/wiki/TreeManager
    - https://github.com/cities-skylines/Assembly-CSharp/wiki/TreeInstance
    - https://github.com/cities-skylines/Assembly-CSharp/wiki/Array32%601
    
    TreeManager - or rather Singleton<TreeManager>.instance - keeps an Array32
    of TreeInstance objects (m_trees). Each tree is referred to by its index
    number in this Array32, with 0 meaning it doesn't exist. Unfortunately,
    Array32 is a sparse array that doesn't let us iterate over its items.
    Fortunately, looping over each possible index and checking the x/z coords
    against 0 does find all the trees with little to no problem.
    
    TreeManager also stores a 540x540 grid of tree indices (m_treeGrid), and
    each TreeInstance seems to hold an index to the "next" tree in its grid
    position (m_nextGridTree), but this isn't really useful for what I'm doing.
*/ 
            var trees = tm.m_trees;
            var treeEntries = new TreeEntry[tm.m_treeCount];
            var toRemove = new uint[tm.m_treeCount];

            uint treeEntryCount = 0;
            uint toRemoveCount = 0;

            for (uint i=1; i < trees.m_size; i++) {
                short x = trees.m_buffer[i].m_posX;
                short z = trees.m_buffer[i].m_posZ;
                if (x != 0 || z != 0) {
                    // Search through treeEntries for an entry with the same
                    // x and z values.
                    bool treeEntryMatched = false;
                    for (uint j=0; j < treeEntryCount; j++) {
                        if (treeEntries[j].x == x && treeEntries[j].z == z) {
                            treeEntryMatched = true;
                            break;
                        }
                    }
                    // If the tree does overlap with one we've already found,
                    // queue it up for removal. If not, add it to treeEntries.
                    if (treeEntryMatched) {
                        toRemove[toRemoveCount] = i;
                        toRemoveCount++;
                    } else {
                        treeEntries[treeEntryCount].id = i;
                        treeEntries[treeEntryCount].x = x;
                        treeEntries[treeEntryCount].z = z;
                        treeEntryCount++;
                    }
                }
            }

            Util.Log(treeEntryCount.ToString() + " single trees found.");
            Util.Log(toRemoveCount.ToString() + " duplicate trees found.");

            if (toRemoveCount > 0) {
                var oldCount = tm.m_treeCount;
                for (uint i=0; i < toRemoveCount; i++) {
                    tm.ReleaseTree(toRemove[i]);
                }
                var newCount = tm.m_treeCount;
                if (oldCount == newCount) {
                    Util.Log("Failed to remove the duplicate trees.");
                } else {
                    if (newCount == treeEntryCount) {
                        Util.Log(toRemoveCount.ToString() +
                            " trees removed successfully.");
                    } else {
                        Util.Log("Something went wrong.");
                        Util.Log("oldCount = " + oldCount.ToString());
                        Util.Log("newCount = " + newCount.ToString());
                    }
                }
            }
        }
    }
}
