using Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Shared.Utils
{
    /// <summary>
    /// Class storing the information that needs to be saved and loaded back with project
    /// </summary>
    public class ProjectMetaData
    {
        public Visibility templateVisibility { get; set; }
        public TemplateChoice templateChoice { get; set; }
        public Visibility backgroundVisibility = Visibility.Collapsed;
        public WriteableBitmap bgImage = null;

        public ProjectMetaData(Visibility templateVisibility, TemplateChoice templateChoice)
        {
            this.templateVisibility = templateVisibility;
            this.templateChoice = templateChoice;
        }

        public ProjectMetaData(Visibility templateVisibility, TemplateChoice templateChoice, Visibility bgVisibility, WriteableBitmap image)
        {
            this.templateVisibility = templateVisibility;
            this.templateChoice = templateChoice;
            this.backgroundVisibility = bgVisibility;
            this.bgImage = image;
        }
    }
}
