using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Input.Inking;

namespace Shared.Models
{
    public class MainCanvasParams
    {
        public List<InkStrokeContainer> strokes;
        public StorageFolder folder;
        public TemplateChoice template;

        public MainCanvasParams(List<InkStrokeContainer> MCStrokes, StorageFolder MCFolder, TemplateChoice chosenTemplate)
        {
            strokes = MCStrokes;
            folder = MCFolder;
            template = chosenTemplate;
        }

    }
}
