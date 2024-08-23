//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using SpawnDev.EBML;
//using SpawnDev.EBML.Elements;

//namespace ConsoleEBMLViewer
//{
//    public class MenuOption
//    {
//        public string Name { get; set; }
//        public Action Action { get; set; }
//        public Func<bool> Available { get; set; }
//        public bool IsGlobal { get; set; }
//    }
//    public class EBMLConsole
//    {
//        public EBMLParser? ebml = null;
//        public Document? Document = null;
//        public FileStream? fileStream = null;
//        public string CurrentMenu = "";
//        public Dictionary<string, List<MenuOption>> Menus = new Dictionary<string, List<MenuOption>>();
//        public async Task RunAsync(string[]? args = null)
//        {
//            // Create the EBML parser with default configuration
//            // default configuration supports matroska and webm reading and modification
//            ebml = new EBMLParser();
//            Menus.Add("main", new List<MenuOption>
//            {
//                new MenuOption{
//                    Name = "Main menu",
//                    Available = () => fileStream == null
//                },
//                new MenuOption{
//                    Name = "Open file",
//                    Available = () => fileStream == null
//                },
//                new MenuOption{
//                    Name = "Close file",
//                    Available = () => fileStream == null
//                },
//            });
//            SetMenu();
//        }
//        void DrawMenu()
//        {
//            if (!Menus.TryGetValue(CurrentMenu, out var menu))
//            {
//                if (Menus.Count > 0)
//                {
//                    SetMenu(Menus.First().Key);
//                }
//                return;
//            }

//        }
//        void SetMenu(string menuName)
//        {
//            if (Menus.TryGetValue(menuName, out var menu))
//            {
//                CurrentMenu = menuName;
//                DrawMenu();
//            }
//        }
//        void OpenFile()
//        {
//            // get a stream containing an EBML document (or multiple documents)
//            var fileStream = File.Open(@"TestData/Big_Buck_Bunny_180 10s.webm", FileMode.Open);
//            // parse the EBML document stream (ParseDocuments can be used to parse all documents in the stream)
//            var document = ebml.ParseDocument(fileStream);
//            if (document != null)
//            {
//                Console.WriteLine($"DocType: {document.DocType}");
//                // or using path
//                Console.WriteLine($"DocType: {document.ReadString(@"/EBML/DocType")}");

//                // Get an element using the path
//                var durationElement = document.GetElement<FloatElement>(@"/Segment/Info/Duration");
//                if (durationElement != null)
//                {
//                    var duration = durationElement.Data;
//                    var durationTime = TimeSpan.FromMilliseconds(duration);
//                    Console.WriteLine($"Duration: {durationTime}");
//                }
//            }

//            // Create a new matroska EBML file
//            var matroskaDoc = ebml.CreateDocument("matroska");
//            Console.WriteLine($"DocType: {matroskaDoc.DocType}");

//            // ...

//        }
//    }
//}
