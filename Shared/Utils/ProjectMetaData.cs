using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml;
using static Protocol.MainCanvas;

namespace Shared.Utils
{
    public class ProjectMetaData
    {
        public Visibility templateVisibility { get; set; }
        public TemplateChoice templateChoice { get; set; }

        public ProjectMetaData(Visibility templateVisibility, TemplateChoice templateChoice)
        {
            this.templateVisibility = templateVisibility;
            this.templateChoice = templateChoice;
        }
    }
}
