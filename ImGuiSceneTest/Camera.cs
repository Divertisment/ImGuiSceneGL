using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImGuiSceneTest {
    public partial class Camera :IMemObj {
        CameraOffsets offs; //FrameCache?
        public Vector2 cursor_sp => offs.Cursor_SP;
        public Vector3 curs_to_world => new Vector3(offs.Cursor_to_world.X, offs.Cursor_to_world.Y, hero_pos.Z);
        public Vector2 curs_to_grid => new Vector2(offs.Cursor_to_world.X, offs.Cursor_to_world.Y) * 0.092f; //worldToGridScale

        public Vector3 hero_pos => new Vector3(offs.Hero_x, offs.Hero_y, offs.Hero_z);

        //public Entity target_ent {
        //    get {
        //        var addr = CameraOffsets.Target_addres << 8 | 0x80;
        //        return GetObject<Entity>(addr);
        //    }
        //}

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
      
    }
    public partial class Camera :IMemObj {
        void OnAddressChange() {
            offs = ui.Read<CameraOffsets>(Address);
            HalfHeight = offs.Height * 0.5f;
            HalfWidth = offs.Width * 0.5f;
        }
       

        public int Width => offs.Width;
        public int Height => offs.Height;
        private float HalfWidth { get; set; }
        private float HalfHeight { get; set; }
        public Vector2 Size => new Vector2(Width, Height);
        public float ZFar => offs.ZFar;
        public Vector3 Position => offs.Position;
        public string PositionString => Position.ToString();

        //cameraarray 0x17c
        private System.Numerics.Matrix4x4 Matrix  => offs.MatrixBytes;
        //public Vector2 GridPointToScreen(Vector2 pos) {
        //    var sk = 10.869565f; //GridToWorldScale
        //    return WorldToScreen(new Vector3(pos.X * sk, pos.Y * sk, 0));
        //}
        public unsafe Vector2 WorldToScreen(Vector3 vec /*, Entity Entity*/) {
            try {

                Vector2 result;
                var cord = *(Vector4*)&vec;
                cord.W = 1;
                cord = Vector4.Transform(cord, Matrix);
                cord = Vector4.Divide(cord, cord.W);
                result.X = (cord.X + 1.0f) * HalfWidth;
                result.Y = (1.0f - cord.Y) * HalfHeight;
                return result;
            }
            catch(Exception ex) {
                ui.AddToLog("Camera WorldToScreen err: "+ex.Message);
            }

            return Vector2.Zero;
        }

    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public partial struct CameraOffsets {
        [FieldOffset(0x8)] public int Width;
        [FieldOffset(0xC)] public int Height;
        [FieldOffset(0x1C4)] public float ZFar;

        //First value is changing when we change the screen size (ratio)
        //4 bytes before the matrix doesn't change
        [FieldOffset(0x80)] public Matrix4x4 MatrixBytes;
        [FieldOffset(0xF0)] public Vector3 Position;
    }
    public partial struct CameraOffsets {
        [FieldOffset(0x39C)] public Vector2 Cursor_SP; //cursor screen point in px
        [FieldOffset(0x3A4)] public Vector2 Cursor_to_world;
        [FieldOffset(0x400)] public float Hero_z;
        [FieldOffset(0x400)] public float Hero_x;
        [FieldOffset(0x400)] public float Hero_y;
        [FieldOffset(0x3c9)] public long Target_addres;
    }
}
