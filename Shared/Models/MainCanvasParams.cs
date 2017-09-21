using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Input.Inking;

namespace Shared.Models
{
    /// <summary>
    /// Class that is the information package that needs to be passed from startpage to main canvas -> viewmodel
    /// </summary>
    public class MainCanvasParams
    {
        public List<InkStrokeContainer> strokes;
        public StorageFolder folder;
        public TemplateChoice template;
        public List<CanvasComponent> components;

        public MainCanvasParams(List<InkStrokeContainer> MCStrokes, StorageFolder MCFolder, TemplateChoice chosenTemplate, List<CanvasComponent> canvasComponents)
        {
            strokes = MCStrokes;
            folder = MCFolder;
            template = chosenTemplate;
            components = canvasComponents;
        }

    }
}
