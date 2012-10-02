/*************************************************************************
Copyright (c) 2012 John McCaffery 

This file is part of Routing Project.

Routing Project is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Routing Project is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Routing Project.  If not, see <http://www.gnu.org/licenses/>.

**************************************************************************/

using System;
using OpenMetaverse;
using common;
using common.framework.abs.wrapper;
using common.framework.interfaces.entities;
using common.model.framework.interfaces;
using common.framework.impl.util;
using Diagrams.Common;
using common.Queue;
using Diagrams.Common.interfaces.keytable;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;

namespace Diagrams {
    public partial class MNode : NodeWrapper, IMNode {
        
        private enum HighlghtState { 
            /// <summary>
            /// All routes to a given root are being highlighted.
            /// </summary>
            HighlightingAll, 
            /// <summary>
            /// The route between a given root and target is being higlighted.
            /// </summary>
            HighlightingSingle, 
            //Nothing is being highlighted
            NotHighlighting 
        };

        #region Public Static Fields

        private static IMNodeInternal HighlightRoot {
            get {
                return _globalHighlightRoot;
            }
        }

        private static IMNodeInternal HighlightTarget {
            get {
                return _globalHighlightTarget;
            }
        }

        #endregion

        #region Internal Static Fields

        private static bool GlobalHighlighting {
            get { return _globalHighlightState == HighlghtState.HighlightingSingle || _globalHighlightState == HighlghtState.HighlightingAll; }
        }

        private static bool GlobalHighlightingSingle {
            get { return _globalHighlightState == HighlghtState.HighlightingSingle; }
        }

        private static bool GlobalHighlightingAll {
            get { return _globalHighlightState == HighlghtState.HighlightingAll; }
        }

        #endregion

        #region Private Static Fields

        /// <summary>
        ///   The node to start the highlighting from. Null if highlighting all routes to target.
        /// </summary>
        private static IMNode _globalHighlightRoot;

        /// <summary>
        ///   The node to highlight the route to.
        /// </summary>
        private static IMNode _globalHighlightTarget;

        /// <summary>
        ///   The key of the node to highlight the route to.
        /// </summary>
        private static UUID _globalTargetKey;

        /// <summary>
        ///   The colour to highlight any link that needs to be highlighted.
        /// </summary>
        private static Color _globalHighlightColour;

        /// <summary>
        /// What state the global highlighting is in.
        /// </summary>
        private static HighlghtState _globalHighlightState = HighlghtState.NotHighlighting;

        /// <summary>
        ///   The object to get a lock on before doing any highlighting operation.
        /// </summary>
        private static readonly Object HighlightLock = new Object();

        #endregion

        #region Private Fields

        private IMLink _highlightedLink;

        private HighlghtState _highlightState = HighlghtState.NotHighlighting;

        #endregion

        #region Private Properties

        private bool Highlighting {
            get { return _highlightState == HighlghtState.HighlightingSingle || _highlightState == HighlghtState.HighlightingAll; }
        }

        private bool HighlightingSingle {
            get { return _highlightState == HighlghtState.HighlightingSingle; }
        }

        private bool HighlightingAll {
            get { return _highlightState == HighlghtState.HighlightingAll; }
        }

        #endregion

        #region IMNodeInternal Highlight Methods

        /// <inhertidoc />
        public void TriggerHighlight(string alg, IMNodeInternal target) {
            if (Equals(_globalHighlightTarget) || Equals(_globalHighlightRoot)) {
                TriggerHighlightReset();
                return;
            }

            ClearOld();

            _globalTargetKey = target.ID;
            _globalHighlightTarget = (IMNode)target;
            _globalHighlightRoot = this;
            _globalHighlightColour = Colour;
            _globalHighlightState = HighlghtState.HighlightingSingle;
            _highlightState = HighlghtState.HighlightingSingle;

            //If there is a route
            if (ForwardingTable.ContainsKey(_globalTargetKey)) {
                lock (HighlightLock) {
                    _highlightedLink = ForwardingTable[_globalTargetKey];
                    _highlightedLink.Colour = _globalHighlightColour;
                    IMNode node = ForwardingTable[_globalTargetKey].OtherEnd(this);
                    node.Highlight(this);
                }
            } else
                _highlightedLink = null;
        }

        /// <inhertidoc />
        public void TriggerHighlightAll(string alg) {
            if (Equals(_globalHighlightTarget)) {
                TriggerHighlightReset();
                return;
            }

            ClearOld();

            _globalTargetKey = ID;
            _globalHighlightTarget = this;
            _globalHighlightRoot = null;
            _globalHighlightColour = Colour;
            _globalHighlightState = HighlghtState.HighlightingAll;

            HighlightAll(this);
        }

        /// <inhertidoc />
        public void TriggerHighlightReset() {
            if (OnHighlightReset != null)
                OnHighlightReset(); 
            _globalHighlightRoot = null;
            _globalHighlightTarget = null;
            _globalHighlightState = HighlghtState.NotHighlighting;
        }

        #endregion

        #region IMNode Highlight Methods

        /// <summary>
        ///   What to do when notified that it should highlight
        /// 
        ///   Will highlight the link and forward the packet on if necessary
        /// </summary>
        /// <param name = "packet">The packet received</param>
        public void Highlight(IMNodeInternal hop) {
            _highlightedLink = ForwardingTable.ContainsKey(_globalTargetKey) ? ForwardingTable[_globalTargetKey] : null;

            if (HighlightingSingle ||
                (_highlightedLink != null && _highlightedLink.OtherEnd(this).Equals(hop))) 
                return;

            lock (HighlightLock) {
                _highlightState = HighlghtState.HighlightingSingle;

                if (_highlightedLink != null) {
                    _highlightedLink.Colour = _globalHighlightColour;
                    _highlightedLink.OtherEnd(this).Highlight(this);
                }
            }
        }

        /// <summary>
        ///   What to do when a packet relevant to the highlight flooding algorithm is received
        /// 
        ///   If the node has not already received one of the highlight flooding packets flood highlight packets along all links and send a new
        ///   highlight all packet back towards the source of the received packet
        /// </summary>
        /// <param name = "packet"></param>
        public void HighlightAll(IMNodeInternal hop) {
            if (HighlightingAll) return;

            lock (HighlightLock) {
                _highlightedLink = ForwardingTable.ContainsKey(_globalTargetKey) ? ForwardingTable[_globalTargetKey] : null;
                _highlightState = HighlghtState.HighlightingAll;

                if (_highlightedLink != null)
                    _highlightedLink.Colour = _globalHighlightColour;

                foreach (IMLink l in _links) {
                    IMNode neighbour = l.OtherEnd(this);
                    if (!neighbour.Equals(hop))
                        neighbour.HighlightAll(this);
                }
            }
        }

        public void ResetHighlight(IMNodeInternal hop) {
            if (Equals(_globalHighlightTarget) ||
                !Highlighting ||
                (_highlightedLink != null && _highlightedLink.OtherEnd(this).Equals(hop)))
                return;

            lock (HighlightLock) {
                _highlightState = HighlghtState.NotHighlighting;

                if (_highlightedLink != null) {
                    _highlightedLink.Reset();
                    _highlightedLink.OtherEnd(this).ResetHighlight(this);
                    _highlightedLink = null;
                }
            }
        }

        private static event Action OnHighlightReset;

        public void ResetHighlightAll() {
            _highlightState = HighlghtState.NotHighlighting;

            if (_highlightedLink != null) {
                _highlightedLink.Reset();
                _highlightedLink = null;
            }
        }

        #endregion 

        #region Private Highlight Methods

        private void RouteChangeListener(string alg, IMNodeInternal target, IMLink oldRoute, IMLink newRoute, float oldDist, float dist) {
            Logger.Debug(Name + " changing route to " + target.Name + ". " + (oldRoute == null ? "No old route" : "Old route: " + oldRoute.Name) + ". " + (newRoute == null ? "No new route" : "New route: " + newRoute.Name));
            if (OnForwardingTableChange != null && (oldRoute == null || newRoute == null || !oldRoute.Equals(newRoute) || oldDist != dist)) 
                OnForwardingTableChange(ID, ForwardingTableList);

            //If not highlighting or not highlighting the route that was changed quit
            if ((oldRoute != null && newRoute != null && oldRoute.Equals(newRoute)) ||
                !GlobalHighlighting ||                
                !target.Equals(_globalHighlightTarget) ||
                (GlobalHighlightingSingle && !HighlightingSingle && !Equals(_globalHighlightRoot)))
                return;

            if (oldRoute != null) {
                oldRoute.Reset();
                if (HighlightingSingle) 
                    oldRoute.OtherEnd(this).ResetHighlight(this);
            }
            
            if (newRoute != null) {
                newRoute.Colour = _globalHighlightColour;
                if (GlobalHighlightingSingle) {
                    _highlightState = HighlghtState.HighlightingSingle;
                    newRoute.OtherEnd(this).Highlight(this);
                }
            } else
                _highlightState = HighlghtState.NotHighlighting;

            _highlightedLink = newRoute;
        }

        private void ClearOld() {
            if (OnHighlightReset != null)
                OnHighlightReset();
        }

        public void Reset(string algorithm) {
            this.Reset();
            if (!algorithm.Equals(_currentAlgorithm))
                return;

            TriggerHighlightReset();
        }

        #endregion
    }
}