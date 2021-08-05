using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using ImGuiScene;
using System.Windows.Forms;
using System.Diagnostics;
using ProcessMemoryUtilities.Memory;
using System.Drawing;
using V2 = System.Numerics.Vector2;
using V3 = System.Numerics.Vector3;
using System.Numerics;

namespace ImGuiSceneTest {
    class ui {
        #region fields
        static SimpleImGuiScene scene;
        static bool b_contr_alt => b_ctrl && b_alt;
        static bool b_alt => Keyboard.IsKeyDown(Keys.LMenu);
        static bool b_ctrl => Keyboard.IsKeyDown(Keys.LControlKey);
        static bool b_was_copy, b_show_log = true, draw_children=true,  b_can_click = true;
        static bool b_ready => poe != null && game_ui != null && game_ui.IsValid;
        static Process poe;
        public static Element game_ui;//i's IngameStateOffsets.IngameUi, NOT IngameStateOffsets.UIRoot
        public static Element ui_root;
        // [FieldOffset(0x5C0)] public long UIRoot; =>wrong?
        public static Camera camera;
        static IntPtr OpenProcessHandle; 
        #endregion

        static void Main(string[] args) {
            CheckPOE();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            scene = SimpleImGuiScene.CreateOverlay(RendererFactory.RendererBackend.OpenGL3);

            //scene.OnBuildUI +=  ImGui.ShowDemoWindow;
            scene.OnBuildUI += Draw;
            scene.Run();

        }
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e) {
            scene.Dispose();
        }
        static double max_elaps = 0;
        static void Draw() {
            b_was_copy = false;

            if(b_contr_alt || b_can_click) {
                scene.Window.SetNotTransperent();
            }
            else
                scene.Window.MakeTransparent();

            #region settings
            ImGui.Begin("Settings", ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.Checkbox("CanClick", ref b_can_click);
            ImGui.Checkbox("draw children", ref draw_children);
            ImGui.End();
            #endregion
            #region tree
            if(!ui.b_contr_alt)
                ImGui.PushStyleColor(ImGuiCol.WindowBg, Color.FromArgb(100, 5, 20, 5).ToImgui());
            ImGui.Begin(
                "Debug ui_root at: "+ui.ui_root.Address_hex,
                    ImGuiWindowFlags.HorizontalScrollbar
                | ImGuiWindowFlags.AlwaysVerticalScrollbar
                );

            frames.Clear();
            if(b_ready) {
                sw.Restart();
                AddToTree(game_ui);
                var elaps = sw.Elapsed.TotalMilliseconds;
                if(elaps > max_elaps)
                    max_elaps = elaps;
                ui.AddToLog("AddToTree max=[" + Math.Round(max_elaps, 3) + "]ms");
                ui.AddToLog("AddToTree time=[" + Math.Round(elaps, 3)+"]ms");
            }
              
            ImGui.TreePop();
            ImGui.End();
            if(!ui.b_contr_alt)
                ImGui.PopStyleColor();
            #endregion
            #region log

            if(!ui.b_contr_alt)
                ImGui.PushStyleColor(ImGuiCol.WindowBg, Color.FromArgb(100, 5, 5, 5).ToImgui());
            //ImGui.SetNextWindowPos(new Vector2(rect.Width / 2 + rect.Left + 100, rect.Top + 110));
            if(ui.b_alt)
                ImGui.Begin("Master INFO",
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.AlwaysAutoResize);
            else
                ImGui.Begin("Master INFO",
                ImGuiWindowFlags.NoInputs |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.AlwaysAutoResize);


            if(ImGui.Button("log")) {
                ui.b_show_log = !ui.b_show_log;
            }
            ImGui.SameLine();
            if(ImGui.Button("Clear")) {
                ui.log.Clear();
            }

            var sb = new StringBuilder();
            foreach(var l in ui.log) {
                if(l.Item2 == 0)
                    sb.Append(l.Item1 + "\n");
                else
                    sb.Append(l.Item1 + " (" + l.Item2 + ")\n");
            }
            if(ui.b_show_log) {
                ImGui.Text(sb.ToString());
            }

            ImGui.End();
            if(!ui.b_contr_alt)
                ImGui.PopStyleColor(); 
            #endregion
            #region frames
            ImGui.SetNextWindowContentSize(ImGui.GetIO().DisplaySize);
            ImGui.SetNextWindowPos(new V2(ui.w_offs.X, ui.w_offs.Y));
            ImGui.Begin(
                "Background Screen",
                    ImGuiWindowFlags.NoInputs |
                    ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                    ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoSavedSettings |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoTitleBar);
            var bptr = ImGui.GetWindowDrawList();

            foreach(var f in frames.Values) {
                bptr.AddRect((new V2(f.Left, f.Top) / ui.screen_k) + ui.w_offs,
                    (new V2(f.Right, f.Bottom) / ui.screen_k) + ui.w_offs,
                    Color.Yellow.ToImgui(), 0, ImDrawCornerFlags.None, 0);
            }
            ui.AddToLog("frames=[" + frames.Count + "]");
            ImGui.End(); 
            #endregion

        }
        static Dictionary<string, Element> direct = new Dictionary<string, Element>();
        static Dictionary<string, Element> roots = new Dictionary<string, Element>();
        #region LOG
        static FixedSizedLog log { get; } = new FixedSizedLog(30);
        public static void ClearLog() {
            log.Clear();
        }
        public static void AddToLog(string str, bool b_error = false) {
            log.Add(str);
        }
        #endregion
        #region Screen utils
       
        public static float screen_k => EXT.GetScreenScalingFactor();
        public static V2 w_offs {
            get {
                var add = new V2(7, 32);
                if(full_screen)
                    add = V2.Zero;
                return new V2(w_rect.Left + add.X, w_rect.Top + add.Y);
            }
        }
        static bool full_screen {
            get {
                if(poe == null) return true;
                var pw_rect = EXT.GetWindowRectangle(poe.MainWindowHandle);
                return Screen.PrimaryScreen.WorkingArea.Width == pw_rect.Width
                    && Screen.PrimaryScreen.WorkingArea.Height == pw_rect.Height;
            }
        }
        static Rectangle w_rect => EXT.GetWindowRectangle(poe.MainWindowHandle);
        /// <summary>
        /// area of the screen in which you can safely move the mouse programmatically
        /// </summary>
        public System.Drawing.Rectangle w_rect_safe {
            get {
                var okw = w_rect;
                int offset = okw.Height / 16;
                okw.Inflate(-offset, -offset);
                okw.Height = okw.Height - okw.Height / 9;
                if(!full_screen) {
                    okw.Location = new System.Drawing.Point(okw.Location.X, okw.Location.Y + 32);
                    okw.Height = okw.Height - 32;
                }
                return okw;
            }
        }

        #endregion

        static long igs_addr, game_ui_addr, ui_root_addr;
        static void CheckPOE() {
            var pp_name = "PathOfExile";
            var pa = Process.GetProcessesByName(pp_name);
            if(pa.Length == 0) {
                ui.AddToLog("CheckPOE err: not found process: " + pp_name);
                return;
            }
            else
                ui.AddToLog("CheckPOE: reading was ok: " + pp_name);
            ui.AddToLog("Use a  Control + Alt keys to activate the ability to move and interact with frames");
            poe = pa[0];
            OpenProcessHandle = ProcessMemory.OpenProcess(ProcessAccessFlags.VirtualMemoryRead, poe.Id);
            var poe_base = poe.MainModule.BaseAddress.ToInt64();
            var gs_offs = 0x0263AEC8; //GameStateOffset
            igs_addr = Read<long>(poe_base + gs_offs, 8, 0);// gc.Game.IngameState.Address pass 3.15.1
            var igs_addr_hex = igs_addr.ToString("X"); //2506031B0C0

            game_ui_addr = Read<long>(igs_addr + 0x98);
            var game_ui_hex = game_ui_addr.ToString("X");
            game_ui = GetObject<Element>(game_ui_addr);
         
            var test = GetOffs(igs_addr, game_ui_addr);
            var cam_addr = Read<long>(poe_base + gs_offs, 8, 0) + 0x788;
            var cam_addr_hex = cam_addr.ToString("X"); 
            camera = GetObject<Camera>(cam_addr);
          
            ui_root_addr = Read<long>(igs_addr + 0x5C0);
            ui_root = GetObject<Element>(ui_root_addr);
        }
  
        static string GetOffs(long start,  long addr_i_need) {
            var res = "";
            var n = 0;
            for(var i = start; i < start + 0x8000; i += 8) {
                var addr = Read<long>(i);
                var ne = GetObject<Element>(addr);
                if(addr > 0 && ne.IsValid) {
                    res = (i - start).ToString("X"); //B8 //118 //298 //2B8
                    direct[res]=ne;
                    Element root = ne;
                    while(root.Parent != null) {
                        root = root.Parent;
                    }
                    var b_root_have_link_to_game_ui = root.Children.FirstOrDefault(ch => ch.Address == game_ui.Address) != null;
                    if(b_root_have_link_to_game_ui) {
                       // ui.AddToLog("Found ui_root at: " + res);
                    }
                    root.IngameStateOffsets_offs = res;
                    roots[root.Address.ToString("X")] = root;
                }
                if(addr == addr_i_need) {
                    res = (i - start).ToString("X");
                    break;
                }
                n += 1;
            }
            return res;
        }
        #region Mem
        public static string ReadString(long address,  int length = 256) {
            var size = Read<uint>(address + 0x10);
            var capacity = Read<uint>(address + 0x18);

            return ReadStringU(8 <= capacity ? Read<long>(address) : address, length);
        }

        public static string ReadStringLong(long address,  int length = 512) {
            var size = Read<int>(address + 0x10) * 2;
            var capacity = Read<uint>(address + 0x18);

            return ReadStringU(8 <= capacity ? Read<long>(address) : address, length);
        }
        public static T AsObject<T>(long _addr) where T : IMemObj, new() {
            var t = new T { Address = _addr };
            return t;
        }
        public static string ReadStringU(long addr, int length = 256, bool replaceNull = true) {
            if(length > 5120 || length < 0)
                return string.Empty;

            if(addr == 0)
                return string.Empty;

            var mem = ReadMem(new IntPtr(addr), length);

            if(mem.Length == 0)
                return string.Empty;

            if(mem[0] == 0 && mem[1] == 0)
                return string.Empty;

            var @string = Encoding.Unicode.GetString(mem);
            return replaceNull ? RTrimNull(@string) : @string;
        }
        private static string RTrimNull(string text) {
            var num = text.IndexOf('\0');
            return num > 0 ? text.Substring(0, num) : text;
        }
        public static T GetObject<T>(long address) where T : IMemObj, new() {
            var t = new T { Address = address };
            return t;
        }

        static T Read<T>(long addr, params int[] offsets) where T : struct {
            return Read<T>(new IntPtr(addr), offsets);
        }

        public static T Read<T>(IntPtr addr, params int[] offsets) where T : struct {
            if(addr == IntPtr.Zero) return default;
            var num = Read<long>(addr);
            var result = num;

            for(var index = 0; index < offsets.Length - 1; index++) {
                if(result == 0)
                    return default;

                var offset = offsets[index];
                result = Read<long>(result + offset);
            }

            if(result == 0)
                return default;

            return Read<T>(result + offsets[offsets.Length - 1]);
        }
        static Stopwatch sw = new Stopwatch();
        public static IList<long> ReadPointersArray(long startAddress, long endAddress, int offset = 8) {
            var result = new List<long>();

            var length = endAddress - startAddress;

            if(length <= 0 || length > 20000 * 8)
                return result;

            sw.Restart();
            result = new List<long>((int)(length / offset) + 1);
            var bytes = ReadMem(startAddress, (int)length);

            for(var i = 0; i < length; i += offset) {
                if(sw.ElapsedMilliseconds > 2000) {
                    ui.AddToLog($"ReadPointersArray error result count = [ {result.Count}]");
                    return new List<long>();
                }

                if(bytes.Length > i + 7)
                    result.Add(BitConverter.ToInt64(bytes, i));
                else
                    ui.AddToLog($"ReadPointersArray error: bytes.Length=[" + (i + 7) + "]");
            }

            return result;
        }
        public static byte[] ReadMem(long addr, int size) {
            return ReadMem(new IntPtr(addr), size);
        }

        public static byte[] ReadMem(IntPtr address, int size) {
            try {
                if(size <= 0 || address.ToInt64() <= 0 /*|| !AddressIsValid(address)*/) return new byte[0];
                var buffer = new byte[size];
                ProcessMemory.ReadProcessMemoryArray(OpenProcessHandle, address, buffer, 0, size);

                // NativeMethods.ReadProcessMemory(SafeHandle, address, buffer, size);
                return buffer;
            }
            catch(Exception e) {
                ui.AddToLog($"Readmem-> A=[ {address.ToString("X")}] Size=[ {size}] {e}");
                throw;
            }
        }
        public static T Read<T>(long addr) where T : struct {
            var ptr = new IntPtr(addr);
            /*if (!AddressIsValid(ptr))
            {
                return default;
            }*/
            return Read<T>(ptr);
        }
        public static T Read<T>(IntPtr addr) where T : struct {
            if(addr == IntPtr.Zero /*|| !AddressIsValid(addr)*/) {
                // throw new Exception($"Invalid address ({addr}) for {typeof(T)}");
                return default;
            }

            var result = new T();
            ProcessMemory.ReadProcessMemory(OpenProcessHandle, addr, ref result);
            return result;
        }
        #endregion
        static Dictionary<string, RectangleF> frames = new Dictionary<string, RectangleF>();
        static Dictionary<long, string> calc = new Dictionary<long, string>();
        public static void AddToTree(Element el) {
            var gui_offs = "";
            var text = "";
            if(el.ChildCount == 0) {
                text = el.Text;
                if(text?.Length > 32)
                    text = text.Substring(0, 32) + "...";
            }
            else
                text = "[" + el.ChildCount + "]";
            if(gui_offs.Length > 0)
                text += "{" + gui_offs + "}";
            var adress = $"{ el.Address:X}";
            if(ImGui.TreeNode($"{adress} { text}")) {
                for(int i = 0; i < el.Children.Count; i++) {
                    var ch = el.Children[i];
                    AddToTree(ch);
                }
                ImGui.TreePop();
            }
            if(ImGui.IsItemHovered( ImGuiHoveredFlags.RootWindow)) {
                AddFrames(el);
                if(!b_was_copy && ImGui.IsMouseClicked(ImGuiMouseButton.Right)) {
                    var str = "";
                    if(el.Text != null && el.Text.Length > 0)
                        str = " " + el.Text;
                    var res = el.Address_hex + str;
                    ImGui.SetClipboardText(res);
                    ui.AddToLog("Cliked on ui.elem=" + res);
                    b_was_copy = true;
                }
                if(!calc.ContainsKey(el.Address)) {//calc offset at game_ui
                    gui_offs = GetOffs(game_ui.Address, el.Address);
                    if(gui_offs.Length > 0) {

                    }
                    calc[el.Address] = gui_offs;
                }
                else
                  if(calc.ContainsKey(el.Address))
                    gui_offs = calc[el.Address];
            }
           
        }
        static int mfst = 60; // max_frames_same_time = 200;
        public static void AddFrames(Element root) {
            if(frames.Count < mfst)
                frames[root.Address_hex] = root.GetClientRect();
            else
                return;
            if(draw_children) {
                foreach(var ch in root.Children) {
                    Debug.Assert(frames.Count < mfst+1);
                    AddFrames(ch);
                }
            }
        }
      
    }
}
