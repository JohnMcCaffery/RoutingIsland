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

using System.Collections.Generic;
using System.Drawing;
using Diagrams;
using OpenMetaverse;
using common.framework.abs.wrapper;
using common.framework.impl.util;
using common.interfaces.entities;
using common.model.framework.interfaces;
using core.view.interfaces;
using common.framework.interfaces.basic;
using common;
using JM726.Lib.Static;
using Diagrams.Framework.Util;
using System.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace core.view.impl.entities {
    // TODO IMPLEMENTED
    public class VNode : PrimWrapper, IVNode {
        private const int BORDER_DEFAULT = 10;
        private const int LINE_HEIGHT_DEFAULT = 9;
        private const int LINE_WIDTH_DEFAULT = 2;
        private const int RESOLUTION_DEFAULT = 256;
        private const int FONT_SIZE_DEFAULT = 6;
        
        private static int Border = 10;
        private static int Line_Height = LINE_HEIGHT_DEFAULT;
        private static int LineWidth = LINE_WIDTH_DEFAULT;
        private static int Resolution = RESOLUTION_DEFAULT;
        private static int FontSize = FONT_SIZE_DEFAULT;

        internal static void SetTableResolution(int resolution) {
            double mult = (double) resolution / (double) RESOLUTION_DEFAULT;
            Border = (int) (BORDER_DEFAULT * mult);
            Line_Height = (int) (LINE_HEIGHT_DEFAULT * mult);
            LineWidth = (int)(LINE_WIDTH_DEFAULT * mult);
            Resolution = (int)(RESOLUTION_DEFAULT * mult);
            FontSize = (int)(FONT_SIZE_DEFAULT * mult);
        }

        private IEnumerable<IPrim> _boards;

        public VNode(IPrimFactory primFactory, string name, Vector3 position, Color colour, Parameters parameters)
            : base(primFactory, position, name, colour, 0d, parameters) {
                
            _boards = new HashSet<IPrim>();
        }

        protected override IPrim InitPrim(IPrimFactory primFactory, Vector3 position) {
            bool isEP = Parameters.Get<bool>("IsEP");
            bool locked = Parameters.Get<bool>("Lock");
            Logger.Info("Creating prim with colour " + DefaultColour);
            IPrim p = primFactory.MakePrim(Name, position, DefaultColour, new Vector3(.5f, .5f, .5f), isEP ? PrimType.Cylinder : PrimType.Sphere, Quaternion.Identity);
            p.Editable = !locked;
            return p;
        }

        #region IVNode Members

        public event PacketReceivedDelegate OnPacketReceived;

        public virtual event EntityMovedDelegate OnAPIMove;

        public Vector3 Pos {
            get { return Prim.Pos; }
            set {
                if (OnAPIMove != null) {
                    Vector3 oldPos = Prim.Pos;
                    Prim.Pos = value;
                    if (!oldPos.Equals(value)) 
                        OnAPIMove(ID, oldPos, value);
                } else
                    Prim.Pos = value; 
            }
        }

        /// <inheritdoc />
        public void PacketReceived(IPacket packet) {
            if (OnPacketReceived != null) {
                OnPacketReceived(ID, packet);
                Logger.Debug(Name + " triggered OnPacketReceived for '" + packet.Name + "'.");
            }
        }

        #endregion

        public override bool Destroy() {
            return Prim.Destroy();
        }

        /// <summary>
        /// Trigger the node to display its forwarding table.
        /// </summary>
        public void DisplayForwardingTable(Route[] routes, IEnumerable<IPrim> boards) {
            _boards = _boards.Concat(boards).Distinct();
            PrintForwardngTable(routes, boards);
        }

        /// <summary>
        /// Update the forwarding table which is being displayed.
        /// </summary>
        /// <param name="routes">The strings which represent each route the forwarding table knows of.</param>
        public void UpdateForwardingTable(Route[] routes) {
            PrintForwardngTable(routes, _boards);
        }

        public void RemoveBoard(UUID board) {
            _boards = _boards.Where(oldBoard => !board.Equals(oldBoard.ID));
        }

        private void PrintForwardngTable(Route[] routes, IEnumerable<IPrim> boards) {
            if (_boards.Count() == 0)
                return;

            int topLine = ((int)(Border + (Line_Height * 1.4)));
            int headers = ((int)(Border + (Line_Height * 1.5)));
            int secondLine = ((int)(Border + (Line_Height * 2.8)));

            int height = secondLine + LineWidth + (Line_Height * routes.Length) + Border;
            Bitmap image = new Bitmap(Resolution, height);
            Graphics graph = Graphics.FromImage(image);
            using (Font font = new Font("Verdana", FontSize, FontStyle.Bold)) {
                //BG
                graph.FillRectangle(new SolidBrush(Color.LightGray), 0, 0, Resolution, Resolution);

                //Title + Underline
                graph.DrawString("Forwarding table for " + Name, font, new SolidBrush(Colour), new Point(Border, Border));
                graph.DrawLine(new Pen(new SolidBrush(Color.Black), LineWidth), new Point(Border, topLine), new Point(Resolution - Border, topLine));

                //Headings + underline
                graph.DrawString("TARGET", font, new SolidBrush(Color.Black), new Point(Border, headers));
                graph.DrawString("HOP", font, new SolidBrush(Color.Black), new Point((((Resolution - (2 * Border)) / 20) * 9), headers));
                graph.DrawString("DIST", font, new SolidBrush(Color.Black), new Point((((Resolution - (2 * Border)) / 10) * 9), headers));
                graph.DrawLine(new Pen(new SolidBrush(Color.Black), LineWidth), new Point(Border, secondLine), new Point(Resolution - Border, secondLine));

                int line = 0;
                for (line = 0; line < routes.Length; line++) {
                    int lineHeight = secondLine + 1 + (Line_Height * line);
                    //Columns
                    graph.DrawString(routes[line].Target, font, new SolidBrush(routes[line].TargetColour), new Point(Border, lineHeight));
                    graph.DrawString(routes[line].Hop, font, new SolidBrush(routes[line].HopColour), new Point((((Resolution - (2 * Border)) / 20) * 9), lineHeight));
                    graph.DrawString(routes[line].Distance.ToString(".###"), font, new SolidBrush(routes[line].TargetColour), new Point((((Resolution - (2 * Border)) / 10) * 9), lineHeight));
                }

                image = new Bitmap(image, new Size(Resolution, Resolution));

                foreach (var board in boards) {
                    float boardHeight = board.Scale.Y / (Resolution / height);                    
                    Vector3 pos = board.IsAttachment ? board.LocalPos : board.Pos;
                    board.Pos = new Vector3(pos.X, pos.Y, (pos.Z + (board.Scale.Z / 2)) - (boardHeight / 2));
                    board.Scale = new Vector3(board.Scale.X, board.Scale.Y, boardHeight);
                    board.Texture = image;
                }
            }
        }
    }
}