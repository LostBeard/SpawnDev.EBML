//namespace BlazorEBMLViewer.Services
//{
//    public class Undoable
//    {
//        public Action DoItCb { get; set; }
//        public Action? UndoItCb { get; set; }
//        public Func<bool>? CanUndoCb { get; set; }
//        public Func<bool>? CanDoCb { get; set; }
//        public string Name { get; set; }
//        public bool CanUndo => !Undone && UndoItCb != null && (CanUndoCb == null || CanUndoCb());
//        public bool CanDo => Undone && DoItCb != null && (CanDoCb == null || CanDoCb());
//        public bool Undone { get; private set; } = true;
//        public bool DoIt()
//        {
//            if (!CanDo) return false;
//            DoItCb();
//            Undone = false;
//            return true;
//        }
//        public bool UndoIt()
//        {
//            if (!CanUndo) return false;
//            UndoItCb!.Invoke();
//            Undone = true;
//            return true;
//        }
//        public Undoable() { }
//        public Undoable(Action doItCb, Action? undoItCb = null) => (DoItCb, UndoItCb) = (doItCb, undoItCb);
//    }
//    //public class AsyncUndoable
//    //{
//    //    public Func<Task> DoItCb { get; set; }
//    //    public Func<Task> UndoItCb { get; set; }
//    //    public Func<Task<bool>> CanUndoCb { get; set; }
//    //    public bool CanUndo => UndoItCb != null &&()
//    //    public bool Undone { get; private set; }
//    //    public string Name { get; set; }
//    //    public void DoIt()
//    //    {
//    //        undoable.DoIt();
//    //        Undone = false;
//    //    }
//    //}
//    public class UndoService
//    {
//        public event Action OnStateHasChanged;
//        public bool CanUndo => Undoable?.CanUndo ?? false;
//        public bool CanRedo => Redoable?.CanDo ?? false;
//        public string UndoName => Undoable?.Name ?? "";
//        public string RedoName => Redoable?.Name ?? "";
//        public Undoable? Undoable => Index >= 0 && Undoables.Count > Index ? Undoables[Index] : null;
//        public Undoable? Redoable => RedoableIndex >= 0 && Undoables.Count > RedoableIndex ? Undoables[RedoableIndex] : null;
//        List<Undoable> Undoables { get; set; } = new List<Undoable>();
//        public int Index
//        {
//            get
//            {
//                for(var i = Undoables.Count - 1; i >= 0; i--)
//                {
//                    var undoable = Undoables[i];
//                    if (!undoable.Undone) return i;
//                }
//                return -1;
//            }
//        }
//        public int RedoableIndex
//        {
//            get
//            {
//                for (var i = Undoables.Count - 1; i >= 0; i--)
//                {
//                    var undoable = Undoables[i];
//                    if (undoable.Undone) return i;
//                }
//                return -1;
//            }
//        }
//        public void StateHasChanged() => OnStateHasChanged?.Invoke();
//        /// <summary>
//        /// Adding a new undoable removes all undone undoables from the stack
//        /// </summary>
//        /// <param name="undoable"></param>
//        public void Do(Undoable undoable)
//        {
//            if (!undoable.DoIt()) return;
//            if (!undoable.CanUndo) return;
//            while (Undoables.Count > 0 && Undoables.Last().Undone)
//            {
//                Undoables.RemoveAt(Undoables.Count - 1);
//            }
//            Undoables.Add(undoable);
//            StateHasChanged();
//        }
//        public void Do(Action doItCb, Action undoItCb)
//        {
//            Do(new Undoable(doItCb, undoItCb));
//        }
//        public void Do(string name, Action doItCb, Action undoItCb)
//        {
//            Do(new Undoable(doItCb, undoItCb) { Name = name });
//        }
//        public bool Redo()
//        {
//            if (!CanRedo) return false;
//            var active = Redoable;
//            var ret = active != null && active.DoIt();
//            StateHasChanged();
//            return ret;
//        }
//        public bool Undo()
//        {
//            if (!CanUndo) return false;
//            var active = Undoable;
//            var ret = active != null && active.UndoIt();
//            StateHasChanged();
//            return ret;
//        }
//        public void Clear()
//        {
//            Undoables.Clear();
//            StateHasChanged();
//        }
//    }
//}
