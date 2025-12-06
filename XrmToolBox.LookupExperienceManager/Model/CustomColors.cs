using System.Drawing;
using System.Windows.Forms;

namespace XrmToolBox.LookupExperienceManager.Model
{
    public class CustomColors: ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(0, 122, 204);
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 50, 60);
        public override Color ImageMarginGradientBegin => Color.FromArgb(45, 50, 60);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 50, 60);
        public override Color ImageMarginGradientEnd => Color.FromArgb(45, 50, 60);
    }
}

