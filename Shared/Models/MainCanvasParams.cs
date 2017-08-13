using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Input.Inking;

namespace Shared.Models
{
    public class MainCanvasParams
    {
        public List<InkStrokeContainer> strokes;
        public StorageFolder folder;
        public enum Template { None, Mobile, Browser };
        public Template template;

        public MainCanvasParams(List<InkStrokeContainer> MCStrokes, StorageFolder MCFolder, Template chosenTemplate)
        {
            strokes = MCStrokes;
            folder = MCFolder;
            template = chosenTemplate;
        }

    }
}
