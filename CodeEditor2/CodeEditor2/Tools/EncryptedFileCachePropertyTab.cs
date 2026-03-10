using Avalonia.Controls;
using Avalonia.Media;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Tools
{
    public class EncryptedFileCachePropertyTab
    {

        TabItem tab;
        Project project;
        ItemPropertyForm form;
        CheckBox enableCheckBox;
        TextBox pathText = new TextBox();
        public EncryptedFileCachePropertyTab(ItemPropertyForm form, Project project)
        {
            this.project = project;
            this.form = form;

            tab = new TabItem() { Name = "EncryptedFileCache", Header = "Cache", FontSize = 14 };
            form.TabControl.Items.Add(tab);

            CodeEditor2.Tools.VerticalGridConstructor gridConstructor = new VerticalGridConstructor();
            tab.Content = gridConstructor.Grid;

            gridConstructor.AppendText("Encripted File Cahse", true);


            enableCheckBox = new CheckBox
            {
                Content = "Activate Local Encripted File Cashe",
                IsChecked = false
            };
            gridConstructor.AppendContol(enableCheckBox,null);
            gridConstructor.AppendText("cashe path");

            pathText = new TextBox()
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };
            gridConstructor.AppendContol(pathText,null);

            loadSetup();
            form.OkButtonControl.Click += OkButtonControl_Click;
        }

        private void loadSetup()
        {
            enableCheckBox.IsChecked = project.LocalFileCasheEnable;
            pathText.Text = project.LocalFileCashePath;
        }
        private void saveSetup()
        {
            if (enableCheckBox.IsChecked == true)
            {
                project.LocalFileCasheEnable = true;
            }
            else
            {
                project.LocalFileCasheEnable = false;
            }
            if(pathText.Text != null)
            {
                project.LocalFileCashePath = pathText.Text;
            }
            else
            {
                project.LocalFileCashePath = "";
            }
        }

        private void OkButtonControl_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            saveSetup();
        }
    }
}
