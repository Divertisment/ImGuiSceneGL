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
    //https://github.com/Queuete/ExileApi/blob/master/GameOffsets/CameraOffsets.cs
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct CameraOffsets {
        [FieldOffset(0x8)] public int Width;
        [FieldOffset(0xC)] public int Height;
        [FieldOffset(0x1C4)] public float ZFar;

        //First value is changing when we change the screen size (ratio)
        //4 bytes before the matrix doesn't change
        [FieldOffset(0x80)] public System.Numerics.Matrix4x4 MatrixBytes;
        [FieldOffset(0xF0)] public Vector3 Position;
    }

}
