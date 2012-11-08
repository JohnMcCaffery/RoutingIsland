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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using common.framework.interfaces.basic;
using OpenMetaverse;
using common.interfaces.entities;
using Diagrams.MRM.Controls.Buttons;
using OpenSim.Region.OptionalModules.Scripting.Minimodule;
using Diagrams.Common;
using Diagrams.MRM.Controls;
using System.Threading;
using System.Drawing;
using Diagrams.MRM;

namespace Diagrams {

    public partial class MRMPrimFactory {
        private readonly Dictionary<uint, TouchButton> _linkButtons;
        private readonly Dictionary<uint, UUID> _linkIDs;
        private readonly Dictionary<string, HashSet<UUID>> _knowButtons;     
        private readonly int _pingChannel;
        private readonly string _ping, _pingAck, _chanAck;

        internal Dictionary<string, HashSet<UUID>> ButtonPrims {
            get { 
                lock (_knowButtons)
                    return new Dictionary<string, HashSet<UUID>>(_knowButtons); 
            }
        }

        internal class ButtonRegisteredArgs : EventArgs {
            internal IPrim Prim;
            internal ButtonRegisteredArgs() { }
            internal ButtonRegisteredArgs(IPrim prim) {
                Prim = prim;
            }
        }

        public event ButtonRegisteredDelegate OnButtonRegistered;

        private readonly Dictionary<UUID, TouchButton> _chatButtons;

        private const string TOUCH = "Touch";

        private void InitTouchButtons() {
            AddChannelListener(_pingChannel, (name, id, text, channel) => {
                if (text.StartsWith(TOUCH) && _chatButtons.ContainsKey(id)) {
                    _chatButtons[id].TriggerTouched(text, id);
                    return;
                }
                if (!text.Equals(_pingAck))
                    return;

                RegisterButtonPrim(name, id);
            });

            _host.Object.Say(_ping, _pingChannel, ChatTargetEnum.LSL);
            _host.Object.Say(_ping, _pingChannel, ChatTargetEnum.MRM);
        }

        public void AddLinkSetRoot(UUID rootID) {
            IObject root = GetIObject(rootID);

            foreach (IObject child in root.Children) 
                RegisterButtonPrim(child.Name, child.GlobalID);

            root.OnTouch += (entity, args) => {
                if (_linkButtons.ContainsKey(args.LinkNumber)) {
                    IObject child = root.Children.FirstOrDefault(obj => obj.LocalID == args.LinkNumber);
                    if (child != null)
                        _linkButtons[args.LinkNumber].TriggerTouched(child, args);
                }
            };
        }

        private void RegisterButtonPrim(string name, UUID id) {
            if (!_knowButtons.ContainsKey(name))
                lock (_knowButtons)
                    _knowButtons.Add(name, new HashSet<UUID>());
            if (_knowButtons[name].Contains(id))
                return;

            lock (_knowButtons)
                _knowButtons[name].Add(id);

            new MRMPrim(id, this).OnWorldDelete += delID => {
                lock (_knowButtons) {
                    _knowButtons[name].Remove(id);
                    if (_knowButtons[name].Count == 0)
                        _knowButtons.Remove(name);
                }
            };

            _logger.Debug("Registered " + name + " as a button.");
            if (OnButtonRegistered != null)
                OnButtonRegistered(id, name);
        }

        internal void RegisterChatButton(UUID prim, TouchButton button) {
            _chatButtons.Add(prim, button);
        }

        internal bool RegisterLinkButton(IObject prim, TouchButton button) {
            if (!prim.Root.GlobalID.Equals(prim.GlobalID)) {
                _linkButtons[prim.LocalID] = button;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Make a button tied to a specific name. Any primitives with the given name and the following script embedded in them will trigger this button when touched.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <returns>A button object that allows listeners to be attached to the button.</returns>
        public IButton MakeButton(string name, IPermissions permissions) {
            IButton button = new TouchButton(this, name, permissions);
            _logger.Info("Made button " + name + " with " + button.Prims.ToArray().Length + " primitives.");
            return button;
        }

        /// <summary>
        /// Make a linked button. If a prim linked to the given root prim is called name then touching it will trigger the button.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <param name="id">The root primitive of the link set. Any children of this primitive with name 'name' will be listened to as a button.</param>
        /// <returns>A button object that allows listeners to be attached to the button.</returns>
        public IButton MakeButton(string name, IPermissions permissions, params UUID[] ids) {
            IButton button = new TouchButton(this, name, permissions, ids);
            _logger.Info("Made button " + name + " with " + button.Prims.Count() + " primitives.");
            return button;
        }

        private Dictionary<Bitmap, UUID> _textureIDs = new Dictionary<Bitmap, UUID>();
        private Dictionary<UUID, Bitmap> _textures = new Dictionary<UUID, Bitmap>();

        internal Bitmap GetTexture(UUID textureID) {
            if (_textures.ContainsKey(textureID))
                return _textures[textureID];
            Bitmap image = _host.Graphics.LoadBitmap(textureID);
            _textures.Add(textureID, image);
            _textureIDs.Add(image, textureID);
            return image;
        }

        internal UUID MakeTexture(Bitmap image) {
            if (_textureIDs.ContainsKey(image))
                return _textureIDs[image];
            UUID textureID = _host.Graphics.SaveBitmap(image);
            _textures.Add(textureID, image);
            _textureIDs.Add(image, textureID);
            return textureID;
        }
    }
}
