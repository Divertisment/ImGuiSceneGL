﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImGuiSceneTest {
    public interface IMemObj {
        long Address{ get; set; }
    }
    public class Element :IMemObj {
        public string IngameStateOffsets_offs;
        ElementOffsets offs ;
        long _address;
        public long Address {
            get => _address;
            set {
                if(_address != value) {
                    _address = value;
                    OnAddressChange();
                }
            }
        }
        public string Address_hex => Address.ToString("X");
        void OnAddressChange() {
            offs = ui.Read<ElementOffsets>(Address);
        }
        private Element _parent;
        public Element Parent => offs.Parent == 0 ? null : (_parent ??= ui.GetObject<Element>(offs.Parent));
        public Vector2 Position => offs.Position;
        public float X => offs.X;
        public float Y => offs.Y;
        public Element Tooltip => Address == 0 ? null : 
            ui.GetObject<Element>(ui.Read<long>(Address + 0x340));
        public float Scale => offs.Scale;
        public float Width => offs.Width;
        public float Height => offs.Height;

        public bool IsValid => offs.SelfPointer == Address;
        public long ChildCount => (offs.ChildEnd - offs.ChildStart) / 8;
        public bool IsVisibleLocal => (offs.IsVisibleLocal & 8) == 8;
        public bool IsVisible {
            get {
                if(Address >= 1770350607106052 || Address <= 0) return false;
                return IsVisibleLocal && GetParentChain().All(current => current.IsVisibleLocal);
            }
        }
        public virtual string Text {
            get {
                var text = ui.ReadString(Address + 0x2E8 + 8);
                return !string.IsNullOrWhiteSpace(text) ? text.Replace("\u00A0\u00A0\u00A0\u00A0", "{{icon}}") : null;
            }
        }

        public IList<Element> Children => GetChildren<Element>();
        public long ChildHash => offs.Childs.GetHashCode();
        public RectangleF GetClientRect() {
            if(Address == 0) return RectangleF.Empty;
            var vPos = GetParentPos();
            float width = ui.camera.Width;
            float height = ui.camera.Height;
            var ratioFixMult = width / height / 1.6f;
            var xScale = width / 2560f / ratioFixMult;
            var yScale = height / 1600f;

            var rootScale = ui.game_ui.Scale;
            var num = (vPos.X + X * Scale / rootScale) * xScale;
            var num2 = (vPos.Y + Y * Scale / rootScale) * yScale;
            return new RectangleF(num, num2, xScale * Width * Scale / rootScale, yScale * Height * Scale / rootScale);
        }
        readonly List<Element> _childrens = new List<Element>();
        long childHashCache;
        Stopwatch sw = new Stopwatch();
        protected List<Element> GetChildren<T>() where T : Element {
            sw.Restart();
            var e = offs;
            if(Address == 0 || e.ChildStart == 0 || e.ChildEnd == 0 || ChildCount < 0) return _childrens;

            if(ChildHash == childHashCache)
                return _childrens;

            var pointers = ui.ReadPointersArray(e.ChildStart, e.ChildEnd);

            if(pointers.Count != ChildCount) return _childrens;
            _childrens.Clear();

            _childrens.AddRange(pointers.Select(ui.GetObject<Element>).ToList());
            childHashCache = ChildHash;
            return _childrens;

        }

     
        public Element Root => ui.ui_root; //must be IngameStateOffsets.UIRoot 
        private IList<Element> GetParentChain() {
            var list = new List<Element>();

            if(Address == 0)
                return list;

            var hashSet = new HashSet<Element>();
            var root = Root;
            var parent = Parent;

            if(root == null)
                return list;

            while(parent != null && !hashSet.Contains(parent) && root.Address != parent.Address && parent.Address != 0) {
                list.Add(parent);
                hashSet.Add(parent);
                parent = parent.Parent;
            }

            return list;
        }
        public Vector2 GetParentPos() {
            float num = 0;
            float num2 = 0;
            var rootScale = ui.game_ui.Scale;
            foreach(var current in GetParentChain()) {
                num += current.X * current.Scale / rootScale;
                num2 += current.Y * current.Scale / rootScale;
            }
            return new Vector2(num, num2);
        }
        public override string ToString() {
           // return Address.ToString("X") + " ch=" + Children.Count; 
            return IngameStateOffsets_offs + " ch=" + Children.Count; 
        }
    }
    //https://github.com/Queuete/ExileApi/blob/master/GameOffsets/ElementOffsets.cs
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct ElementOffsets {
        public const int OffsetBuffers = 0x6EC;

        [FieldOffset(0x28)] public long SelfPointer;
        [FieldOffset(0x30)] public long ChildStart;
        [FieldOffset(0x30)] public NativePtrArray Childs;
        [FieldOffset(0x38)] public long ChildEnd;
        [FieldOffset(0xD8)] public long Root;
        [FieldOffset(0xE0)] public long Parent;
        [FieldOffset(0xE8)] public Vector2 Position;
        [FieldOffset(0xE8)] public float X;
        [FieldOffset(0xEC)] public float Y;
        [FieldOffset(0x158)] public float Scale;
        [FieldOffset(0x161)] public byte IsVisibleLocal;

        [FieldOffset(0x160)] public uint ElementBorderColor;
        [FieldOffset(0x164)] public uint ElementBackgroundColor;
        [FieldOffset(0x168)] public uint ElementOverlayColor;

        [FieldOffset(0x180)] public float Width;
        [FieldOffset(0x184)] public float Height;

        [FieldOffset(0x190)] public uint TextBoxBorderColor;
        [FieldOffset(0x190)] public uint TextBoxBackgroundColor;
        [FieldOffset(0x194)] public uint TextBoxOverlayColor;

        [FieldOffset(0x1C0)] public uint HighlightBorderColor;
        [FieldOffset(0x1C3)] public bool isHighlighted;

        [FieldOffset(0x3E8)] public long Tooltip;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NativePtrArray :IEquatable<NativePtrArray> {
        public readonly long First;
        public readonly long Last;
        public readonly long End;
        public long Size => Last - First;

        public override string ToString() {
            return $"First: {First.ToString("X")}, Last: {Last.ToString("X")}, End: {End.ToString("X")} Size:{Size}";
        }

        public bool Equals(NativePtrArray other) {
            if(First == other.First && Last == other.Last)
                return End == other.End;

            return false;
        }

        public override bool Equals(object obj) {
            if(obj is NativePtrArray other)
                return Equals(other);

            return false;
        }

        public override int GetHashCode() {
            return (((First.GetHashCode() * 397) ^ Last.GetHashCode()) * 397) ^ End.GetHashCode();
        }
    }
   
}
