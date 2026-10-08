using System;
using System.Collections.Generic;

namespace QuickMindMap.Models
{
    public class UndoRedoManager
    {
        private readonly Stack<string> _undoStack = new();
        private readonly Stack<string> _redoStack = new();
        private const int MaxHistory = 60;

        public event EventHandler? StateChanged;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void RecordState(MindMapDocument document)
        {
            try
            {
                string state = document.ToJson();
                _undoStack.Push(state);
                if (_undoStack.Count > MaxHistory)
                {
                    // Drop oldest items by converting to list
                    var list = new List<string>(_undoStack);
                    list.RemoveAt(list.Count - 1);
                    _undoStack.Clear();
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        _undoStack.Push(list[i]);
                    }
                }

                _redoStack.Clear();
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UndoRedo Record Error] {ex.Message}");
            }
        }

        public MindMapDocument? Undo(MindMapDocument currentDocument)
        {
            if (!CanUndo) return null;

            try
            {
                // Save current state to redo stack
                string currentState = currentDocument.ToJson();
                _redoStack.Push(currentState);

                string previousState = _undoStack.Pop();
                var restored = MindMapDocument.FromJson(previousState);
                StateChanged?.Invoke(this, EventArgs.Empty);
                return restored;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Undo Error] {ex.Message}");
                return null;
            }
        }

        public MindMapDocument? Redo(MindMapDocument currentDocument)
        {
            if (!CanRedo) return null;

            try
            {
                // Save current state to undo stack
                string currentState = currentDocument.ToJson();
                _undoStack.Push(currentState);

                string nextState = _redoStack.Pop();
                var restored = MindMapDocument.FromJson(nextState);
                StateChanged?.Invoke(this, EventArgs.Empty);
                return restored;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Redo Error] {ex.Message}");
                return null;
            }
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
