using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Ikiz.App.ViewModels;
using Ikiz.Core;
using Microsoft.Win32;

namespace Ikiz.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private FileItem? _hoverFile;
    private FileRowVm? _menuFile;
    private GroupHeaderVm? _menuGroup;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.PropertyChanged += OnViewModelPropertyChanged;
        ThemeManager.Changed += OnThemeChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.RenameOpen) && _vm.RenameOpen)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                RenameBox.Focus();
                RenameBox.SelectAll();
            });
        }
    }

    private void OnThemeChanged() => _vm.OnThemeChanged();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        if (App.StartupFolder is { } folder) await _vm.AddRootAsync(folder);
        else await _vm.StartAsync();
        _ = _vm.CheckUpdatesSilentlyAsync();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _vm.PropertyChanged -= OnViewModelPropertyChanged;
        ThemeManager.Changed -= OnThemeChanged;
        _vm.Stop();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.K)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (_vm.CompareOpen)
            {
                _vm.CloseCompare();
                e.Handled = true;
                return;
            }

            if (_vm.PrivacyOpen)
            {
                _vm.ClosePrivacy();
                e.Handled = true;
                return;
            }

            if (_vm.LocationsOpen)
            {
                _vm.CloseLocations();
                e.Handled = true;
                return;
            }

            if (_vm.AboutOpen)
            {
                _vm.CloseAbout();
                e.Handled = true;
                return;
            }

            if (_vm.RenameOpen)
            {
                _vm.CloseRename();
                e.Handled = true;
                return;
            }

            if (_vm.DeleteOpen)
            {
                _vm.CloseQuickDelete();
                e.Handled = true;
                return;
            }

            if (_vm.BoardOpen)
            {
                _vm.CloseBoard();
                e.Handled = true;
                return;
            }
        }

        if (Keyboard.FocusedElement is MenuItem)
            return;

        var inTextBox = Keyboard.FocusedElement is TextBox;
        if (!inTextBox && !_vm.CompareOpen && !_vm.RenameOpen && !_vm.DeleteOpen && !_vm.BoardOpen && !_vm.AboutOpen && !_vm.LocationsOpen && !_vm.PrivacyOpen && _hoverFile is not null)
        {
            if (e.Key == Key.F2)
            {
                _vm.OpenRename(_hoverFile);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete)
            {
                _vm.OpenQuickDelete(_hoverFile, "one");
                e.Handled = true;
                return;
            }
        }

        if (e.Key == Key.Oem2 && Keyboard.FocusedElement is not TextBox)
        {
            SearchBox.Focus();
            e.Handled = true;
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnGroupHeaderClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: GroupHeaderVm group })
            _vm.ToggleGroup(group.Key);
    }

    private void OnStageGroupClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string key })
        {
            _vm.StageGroup(key);
            e.Handled = true;
        }
    }

    private void OnTrashRowClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FileRowVm row })
        {
            if (row.File.IsDirectory || _vm.OnlyEmptyItems || _vm.CopyCount(row.File) <= 1)
                _vm.StageFile(row.File);
            else
                _vm.StageGroup(row.File.DuplicateKey);
            e.Handled = true;
        }
    }

    private void OnStageAllClick(object sender, RoutedEventArgs e) => _vm.StageAll();

    private void OnPickCheckClick(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { Tag: FileRowVm row } box)
            _vm.SetPicked(row.File.Id, box.IsChecked == true);
    }

    private void OnPickAllClick(object sender, RoutedEventArgs e) => _vm.PickAll();

    private void OnPickCopiesClick(object sender, RoutedEventArgs e) => _vm.PickCopies();

    private void OnPickEmptyItemsClick(object sender, RoutedEventArgs e) => _vm.PickEmptyItems();

    private void OnClearPicksClick(object sender, RoutedEventArgs e) => _vm.ClearPicks();

    private void OnComparePickedClick(object sender, RoutedEventArgs e) => _vm.OpenCompareForPicked();

    private void OnStagePickedClick(object sender, RoutedEventArgs e) => _vm.StagePicked();

    private void OnDeletePickedClick(object sender, RoutedEventArgs e) => _vm.DeletePicked();

    private void OnRenamePickedClick(object sender, RoutedEventArgs e) => _vm.RenamePicked();

    private void OnOpenBoardClick(object sender, RoutedEventArgs e) => _vm.OpenBoard();

    private void OnCloseBoardClick(object sender, RoutedEventArgs e) => _vm.CloseBoard();

    private void OnScrimClick(object sender, MouseButtonEventArgs e) => _vm.CloseBoard();

    private void OnBoardRowMainClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BoardRowVm row })
            row.IsSelected = !row.IsSelected;
    }

    private void OnKeepOneClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BoardGroupVm group }) _vm.KeepOne(group);
    }

    private void OnUnstageClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BoardRowVm row }) _vm.Unstage(row.File.Id);
    }

    private void OnSelectAllBoardClick(object sender, RoutedEventArgs e) => _vm.SelectAllBoard();

    private void OnSelectNoneBoardClick(object sender, RoutedEventArgs e) => _vm.SelectNoneBoard();

    private void OnClearBoardClick(object sender, RoutedEventArgs e) => _vm.ClearBoard();

    private async void OnConfirmDeleteClick(object sender, RoutedEventArgs e)
        => await _vm.ConfirmDeleteAsync();

    private void OnReindexClick(object sender, RoutedEventArgs e) => _vm.Reindex();

    private void OnOpenLocationsClick(object sender, RoutedEventArgs e) => _vm.OpenLocations();

    private void OnCloseLocationsClick(object sender, RoutedEventArgs e) => _vm.CloseLocations();

    private void OnLocationsScrimClick(object sender, MouseButtonEventArgs e) => _vm.CloseLocations();

    private void OnRefreshDrivesClick(object sender, RoutedEventArgs e) => _vm.RefreshDrives();

    private async void OnAddSelectedDrivesClick(object sender, RoutedEventArgs e)
        => await _vm.AddSelectedDrivesAsync();

    private void OnRemoveRootClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string path }) _vm.RemoveRoot(path);
    }

    private async void OnPickFolderClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Taranacak klasör seç",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };

        if (dialog.ShowDialog(this) == true)
            await _vm.AddLocationsAsync(new[] { dialog.FolderName });
    }

    // ------------------------------------------------------------ sağ tık menüsü

    private void OnFileRowMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: FileRowVm row })
            _hoverFile = row.File;
    }

    private static MenuItem? MenuItemByTag(ContextMenu menu, string tag)
        => menu.Items.OfType<MenuItem>().FirstOrDefault(m => string.Equals(m.Tag as string, tag, StringComparison.Ordinal));

    private static void SetMenuItem(ContextMenu menu, string tag, bool visible = true, bool enabled = true)
    {
        if (MenuItemByTag(menu, tag) is not { } item) return;
        item.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        item.IsEnabled = enabled;
    }

    private void OnFileMenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu) return;
        _menuGroup = null;
        _menuFile = menu.PlacementTarget is FrameworkElement { DataContext: FileRowVm row } ? row : null;
        if (_menuFile is null)
        {
            menu.IsOpen = false;
            return;
        }

        var file = _menuFile.File;
        var inBoard = _vm.BoardContains(file);
        var copyCount = _vm.CopyCount(file);

        SetMenuItem(menu, "stage", visible: !inBoard);
        SetMenuItem(menu, "unstage", visible: inBoard);
        SetMenuItem(menu, "rename", visible: !file.IsDirectory);
        SetMenuItem(menu, "renameType", visible: !file.IsDirectory);
        SetMenuItem(menu, "deleteCopies", visible: !file.IsDirectory && copyCount > 1);
        SetMenuItem(menu, "deleteOthers", visible: !file.IsDirectory && copyCount > 1);
        SetMenuItem(menu, "undo", enabled: _vm.CanUndoDelete);

        if (MenuItemByTag(menu, "undo") is { } undo)
            undo.Header = _vm.UndoMenuText;
    }

    private void OnGroupMenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu) return;
        _menuFile = null;
        _menuGroup = menu.PlacementTarget is FrameworkElement { DataContext: GroupHeaderVm group } ? group : null;
        if (_menuGroup is null)
        {
            menu.IsOpen = false;
            return;
        }

        var isEmptyGroup = _menuGroup.IsEmptyGroup;
        SetMenuItem(menu, "renameGroup", visible: !isEmptyGroup);
        SetMenuItem(menu, "renameType", visible: !isEmptyGroup);
        SetMenuItem(menu, "deleteOthers", visible: !isEmptyGroup);
        SetMenuItem(menu, "undo", enabled: _vm.CanUndoDelete);
        if (MenuItemByTag(menu, "undo") is { } undo)
            undo.Header = _vm.UndoMenuText;
    }

    private void OnMenuOpenLocationClick(object sender, RoutedEventArgs e)
    {
        if (_menuFile is not { } row) return;
        _vm.OpenFileLocation(row.File);
    }

    private void OnMenuOpenFolderClick(object sender, RoutedEventArgs e)
    {
        if (_menuFile is not { } row) return;
        _vm.OpenContainingFolder(row.File);
    }

    private void OnMenuCompareClick(object sender, RoutedEventArgs e)
    {
        if (_menuFile is not { } row) return;
        _vm.OpenCompareForFile(row.File);
    }

    private void OnMenuOpenGroupLocationClick(object sender, RoutedEventArgs e)
    {
        if (_menuGroup is not { } group) return;
        var first = _vm.FirstFileOfGroup(group.Key);
        if (first is not null) _vm.OpenFileLocation(first);
    }

    private void OnMenuCompareGroupClick(object sender, RoutedEventArgs e)
    {
        if (_menuGroup is not { } group) return;
        _vm.OpenCompareForGroup(group.Key);
    }

    private void OnCompareScrimClick(object sender, MouseButtonEventArgs e) => _vm.CloseCompare();

    private void OnCloseCompareClick(object sender, RoutedEventArgs e) => _vm.CloseCompare();

    private void OnCompareKeepOldestClick(object sender, RoutedEventArgs e) => _vm.CompareKeepOldest();

    private void OnCompareKeepNewestClick(object sender, RoutedEventArgs e) => _vm.CompareKeepNewest();

    private void OnCompareSelectAllClick(object sender, RoutedEventArgs e) => _vm.CompareSelectAll();

    private void OnCompareSelectNoneClick(object sender, RoutedEventArgs e) => _vm.CompareSelectNone();

    private void OnCompareRowOpenLocationClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CompareRowVm row })
            _vm.OpenFileLocation(row.File);
    }

    private void OnStageComparedClick(object sender, RoutedEventArgs e) => _vm.StageCompared();

    private void OnDeleteComparedClick(object sender, RoutedEventArgs e) => _vm.DeleteCompared();

    private void OnMenuStageClick(object sender, RoutedEventArgs e)
    {
        if (_menuFile is not { } row) return;
        _vm.StageFile(row.File);
    }

    private void OnMenuUnstageClick(object sender, RoutedEventArgs e)
    {
        if (_menuFile is not { } row) return;
        _vm.UnstageFile(row.File);
    }

    private void OnMenuRenameClick(object sender, RoutedEventArgs e)
    {
        if (_menuFile is not { } row) return;
        _vm.OpenRename(row.File);
    }

    private void OnMenuRenameTypeClick(object sender, RoutedEventArgs e)
    {
        if (_menuFile is not { } row) return;
        _vm.OpenRename(row.File, "type");
    }

    private void OnMenuDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_menuFile is not { } row) return;
        _vm.OpenQuickDelete(row.File, "one");
    }

    private void OnMenuDeleteOthersClick(object sender, RoutedEventArgs e)
    {
        if (_menuFile is not { } row) return;
        _vm.OpenQuickDelete(row.File, "others");
    }

    private void OnMenuDeleteCopiesClick(object sender, RoutedEventArgs e)
    {
        if (_menuFile is not { } row) return;
        _vm.OpenQuickDelete(row.File, "all");
    }

    private void OnMenuStageGroupClick(object sender, RoutedEventArgs e)
    {
        if (_menuGroup is not { } group) return;
        _vm.StageGroup(group.Key);
    }

    private void OnMenuRenameGroupClick(object sender, RoutedEventArgs e)
    {
        if (_menuGroup is not { } group) return;
        _vm.OpenRenameForGroup(group.Key);
    }

    private void OnMenuRenameGroupTypeClick(object sender, RoutedEventArgs e)
    {
        if (_menuGroup is not { } group) return;
        _vm.OpenRenameForType(group.Key, group.ExtLabel);
    }

    private void OnMenuDeleteOthersGroupClick(object sender, RoutedEventArgs e)
    {
        if (_menuGroup is not { } group) return;
        _vm.OpenQuickDeleteForGroup(group.Key);
    }

    private void OnMenuDeleteGroupClick(object sender, RoutedEventArgs e)
    {
        if (_menuGroup is not { } group) return;
        _vm.OpenQuickDeleteForGroup(group.Key, "all");
    }

    private void OnMenuUndoClick(object sender, RoutedEventArgs e) => _ = _vm.UndoLastDeleteAsync();

    private async void OnToastUndoClick(object sender, RoutedEventArgs e)
        => await _vm.UndoLastDeleteAsync();

    // ------------------------------------------------------------ yeniden adlandırma

    private void OnRenameScrimClick(object sender, MouseButtonEventArgs e) => _vm.CloseRename();

    private void OnRenameCloseClick(object sender, RoutedEventArgs e) => _vm.CloseRename();

    private async void OnApplyRenameClick(object sender, RoutedEventArgs e)
        => await _vm.ApplyRenameAsync();

    // ------------------------------------------------------------ doğrudan silme

    private void OnQuickDeleteScrimClick(object sender, MouseButtonEventArgs e) => _vm.CloseQuickDelete();

    private void OnQuickDeleteCloseClick(object sender, RoutedEventArgs e) => _vm.CloseQuickDelete();

    private async void OnConfirmQuickDeleteClick(object sender, RoutedEventArgs e)
        => await _vm.ConfirmQuickDeleteAsync();

    // ------------------------------------------------------------ hakkında / güncelleme

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        _vm.DismissUpdatePrompt();
        _vm.OpenAbout();
    }

    private async void OnInstallUpdatePromptClick(object sender, RoutedEventArgs e)
        => await _vm.InstallUpdateFromPromptAsync();

    private void OnDismissUpdatePromptClick(object sender, RoutedEventArgs e)
        => _vm.DismissUpdatePrompt();

    private void OnAboutCloseClick(object sender, RoutedEventArgs e) => _vm.CloseAbout();

    private void OnAboutScrimClick(object sender, MouseButtonEventArgs e) => _vm.CloseAbout();

    private void OnAboutProjectClick(object sender, RoutedEventArgs e) => _vm.OpenProjectPage();

    private void OnAboutIssuesClick(object sender, RoutedEventArgs e) => _vm.OpenIssuesPage();

    private void OnAboutReleasesClick(object sender, RoutedEventArgs e) => _vm.OpenReleasesPage();

    private async void OnAboutUpdateClick(object sender, RoutedEventArgs e)
        => await _vm.RunUpdateActionAsync();

    // ------------------------------------------------------------ paylaşım denetimi

    private void OnOpenPrivacyClick(object sender, RoutedEventArgs e) => _vm.OpenPrivacy();

    private void OnClosePrivacyClick(object sender, RoutedEventArgs e) => _vm.ClosePrivacy();

    private void OnPrivacyScrimClick(object sender, MouseButtonEventArgs e) => _vm.ClosePrivacy();

    private void OnRefreshPrivacyClick(object sender, RoutedEventArgs e) => _vm.RefreshPrivacyCheck();

    private void OnCopyPrivacyClick(object sender, RoutedEventArgs e) => _vm.CopyPrivacySummary();
}
