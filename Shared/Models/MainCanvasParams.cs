using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Input.Inking;

namespace Shared.Models
{
    public class MainCanvasParams
    {
        public List<InkStrokeContainer> strokes;
        public StorageFolder folder;
        public CanvasSize size;

        public MainCanvasParams(List<InkStrokeContainer> MCStrokes, StorageFolder MCFolder, CanvasSize PrototypeSize)
        {
            strokes = MCStrokes;
            folder = MCFolder;
            size = PrototypeSize;
        }

    }
}
