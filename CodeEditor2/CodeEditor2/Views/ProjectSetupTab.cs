using Avalonia.Controls;
using Avalonia.Media;
using CodeEditor2.Data;
using CodeEditor2.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Views
{
    public class ProjectSetupTab
    {
        TabItem tab;
        Project project;
        ItemPropertyForm form;
        CodeEditor2.NavigatePanel.NavigatePanelNode node;
        TextBox cashePathText = new TextBox();
        CheckBox casheCheck = new CheckBox() { Content = "Activate json Cashe" };
        public ProjectSetupTab(Project project, ItemPropertyForm form, CodeEditor2.NavigatePanel.NavigatePanelNode node)
        {
            this.project = project;
            this.form = form;
            this.node = node;

            tab = new TabItem() { Name = "project", Header = "Project", FontSize = 14 };
            form.TabControl.Items.Add(tab);

            CodeEditor2.Tools.VerticalGridConstructor gridConstructor = new VerticalGridConstructor();
            tab.Content = gridConstructor.Grid;

            gridConstructor.AppendText("Project Options", true);
            gridConstructor.AppendContol(casheCheck,null);

            gridConstructor.AppendText("Cashe Path");
            gridConstructor.AppendContolFill(cashePathText);

            form.OkButtonControl.Click += OkButtonControl_Click;
//            compileOptionText.Text = projectProperty.CompileOption;
        }
        private void OkButtonControl_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {

            //if (this.compileOptionText.Text == null)
            //{
            //    projectProperty.CompileOption = "";
            //}
            //else
            //{
            //    projectProperty.CompileOption = compileOptionText.Text;
            //}
        }
    }
}
