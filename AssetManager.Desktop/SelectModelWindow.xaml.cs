using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssetManager.Infrastructure.Models;
using AssetManager.Infrastructure.Services;
using Autodesk.DataManagement.Model;

namespace AssetManager.Desktop;

public partial class SelectModelWindow : Window
{
    public string SelectedModel { get; private set; }
    public string SelectedModelName { get; private set; }
    
    private string _selectedItemId;
    private string hubID = MainWindow.selectedHubID; 

    public SelectModelWindow()
    {
        InitializeComponent();
        LoadProjects();
    }

    private async void LoadProjects()
    {
        var projects = await DataManagement.GetAllProjectsFromHub(hubID);
    
        if (projects != null && projects.Count > 0)
        {
            ProjectListBox.Items.Clear(); // Clear previous items

            foreach (var (projectId, projectName) in projects)
            {
                ListBoxItem item = new ListBoxItem
                {
                    Content = projectName,  // Display project name
                    Tag = projectId         // Store project ID
                };
                ProjectListBox.Items.Add(item);
            }
        }
        else
        {
            MessageBox.Show("No projects found or failed to load.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ProjectListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (ProjectListBox.SelectedItem is ListBoxItem selectedItem)
        {
            string selectedProjectId = selectedItem.Tag as string;

            if (!string.IsNullOrEmpty(selectedProjectId))
            {
                var (folderId, folderName) = await DataManagement.GetTopLevelFolder(hubID, selectedProjectId);

                if (string.IsNullOrEmpty(folderId))
                {
                    MessageBox.Show("No top-level folder found for this project.", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var models = await DataManagement.GetProjectItems(selectedProjectId, folderId);
                
                ModelListBox.Items.Clear();
                foreach (var item in models)
                {
                    ListBoxItem itemEntry = new ListBoxItem
                    {
                        Content = item.ItemName,
                        Tag = item.ItemId
                    };

                    if (item.IsFolder)
                    {
                        itemEntry.FontWeight = FontWeights.Bold;
                    }

                    ModelListBox.Items.Add(itemEntry);
                }

            }
        }
    }

    private async void ModelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModelListBox.SelectedItem is ListBoxItem selectedItem)
        {
            _selectedItemId = selectedItem.Tag as string;
        }
    }

    private void SelectModelButton_Click(object sender, RoutedEventArgs e)
    {
        if (ModelListBox.SelectedItem is ListBoxItem selectedModel)
        {
            SelectedModel = selectedModel.Name;
            SelectedModel = selectedModel.Tag as string;  // id
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Please select a model.", "No Model Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}