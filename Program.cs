using Array = System.Array;
using StringComparison = System.StringComparison;
using Gtk;
using Application = Gtk.Application;

string versionApplication = File.ReadAllText(FastPath("version"));
string[] dataApplication = File.ReadAllLines(FastPath("data"));

string[] defdata =
[
    "MyApp",
    "My Application",
    "1.0.0",
    "amd64",
    "Author",
    "Description Application",
    "/home/user/MyApp",
    "/home/user/MyApp/icon.svg",
    "/opt/MyApp",
    "MyApp",
    "Utility"
];

if (dataApplication.Length < 11)
    dataApplication = defdata;

Application.Init();

var root = new Window("LMDB");
root.DeleteEvent += (o, e) => Application.Quit();
root.SetSizeRequest(800, 700);
root.SetPosition(WindowPosition.Center);
root.SetIconFromFile(FastPath("logo.png"));
root.Resizable = false;

var stack = new Stack
{
    TransitionDuration = 500,
    TransitionType = StackTransitionType.SlideLeft
};

var content = new VBox { Margin = 15 };
var building = new VBox { Margin = 15 };
var about = new VBox { Margin = 15 };

stack.Add(content);
stack.Add(building);
stack.Add(about);

var logo = new Image(FastPath("logo.png"));

var packageName = new Entry(dataApplication[0]);
var packageNameText = new Label("Package name") { Xalign = 0 };

var applicationName = new Entry(dataApplication[1]);
var applicationNameText = new Label("Application name") { Xalign = 0 };

var version = new Entry(dataApplication[2]);
var versionText = new Label("Version") { Xalign = 0 };

string[] architectures =
[
    "amd64",
    "arm64",
    "armhf",
    "i386",
    "all"
];

var architecture = new ComboBoxText();

foreach (var item in architectures)
    architecture.AppendText(item);

int archIndex = Array.FindIndex(
    architectures,
    x => x.Equals(dataApplication[3], StringComparison.OrdinalIgnoreCase));

architecture.Active = archIndex >= 0 ? archIndex : 0;

var architectureText = new Label("Architecture") { Xalign = 0 };

var authors = new Entry(dataApplication[4]);
var authorText = new Label("Author") { Xalign = 0 };

var description = new Entry(dataApplication[5]);
var descriptionText = new Label("Description") { Xalign = 0 };

var folderPath = new Entry(dataApplication[6]);
var folderText = new Label("Folder path*") { Xalign = 0 };

var executable = new Entry(dataApplication[9]);
var executableText = new Label("Executable name*") { Xalign = 0 };

var iconPath = new Entry(dataApplication[7]);
var iconPathText = new Label("Icon path*") { Xalign = 0 };

var installPath = new Entry(dataApplication[8]);
var installPathText = new Label("Install path*") { Xalign = 0 };

string[] categories =
[
    "AudioVideo",
    "Audio",
    "Video",
    "Development",
    "Education",
    "Game",
    "Graphics",
    "Network",
    "Office",
    "Science",
    "Settings",
    "System",
    "Utility",
    "Other"
];

var category = new ComboBoxText();

foreach (var item in categories)
    category.AppendText(item);

int catIndex = Array.FindIndex(
    categories,
    x => x.Equals(dataApplication[10], StringComparison.OrdinalIgnoreCase));

category.Active = catIndex >= 0 ? catIndex : 12;

var categoryText = new Label("Category") { Xalign = 0 };

var btnContainer = new HBox();
var btnBuild = new Button("Build");
var btnSetDef = new Button("Set all default");
var btnAbout = new Button("?");

btnContainer.PackStart(btnBuild, true, true, 0);
btnContainer.PackStart(btnSetDef, false, true, 0);
btnContainer.PackStart(btnAbout, false, true, 0);

var spinner = new Spinner();
spinner.Start();

var buildTextLoading = new Label("Building...");

var logoAbout = new Image(FastPath("logo.png")) { Margin = 10 };

var authorProgram = new Label();
authorProgram.Markup = "<span font_size=\"15000\">Author : Skvik360</span>";

var versionAbout = new Label();
versionAbout.Markup =
    $"<span font_size=\"10000\">Version : {versionApplication}</span>";

var textAbout = new Label { Xalign = 0 };
textAbout.Markup =
    "\nLMDB is a GUI application that allows you to easily create `.deb` packages.\n" +
    "\nThe application is primarily designed for Linux Mint, so compatibility with other Debian-based distributions is not guaranteed.\n" +
    "<b>Package Name</b> — The unique system name of the package.\n" +
    "\n<b>Application Name</b> — The name displayed in the application menu.\n" +
    "\n<b>Version</b> — The package version.\n" +
    "\n<b>Architecture</b> — The CPU architecture of the application.\n" +
    "\n<b>Author</b> — The author or developer of the application.\n" +
    "\n<b>Description</b> — A short description of the application.\n" +
    "\n<b>Folder Path</b> — The path to the folder containing the application files.\n" +
    "\n<b>Executable Name</b> — The main executable file name.\n" +
    "\n<b>Icon</b> — The path to the application icon.\n" +
    "\n<b>Install Path</b> — The directory where the application will be installed.\n" +
    "\n<b>Category</b> — The application category.";

var btnBackContent = new Button("Back");

btnBuild.Clicked += async (o, e) =>
{
    string[] tempData =
    [
        packageName.Text,
        applicationName.Text,
        version.Text,
        architecture.ActiveText,
        authors.Text,
        description.Text,
        folderPath.Text,
        iconPath.Text,
        installPath.Text,
        executable.Text,
        category.ActiveText
    ];

    File.WriteAllLines(FastPath("data"), tempData);

    stack.VisibleChild = building;

    var result = await Logic.Build(
        tempData[0],
        tempData[1],
        tempData[2],
        tempData[3],
        tempData[4],
        tempData[5],
        tempData[6],
        tempData[9],
        tempData[7],
        tempData[8],
        tempData[10]);

    var dialog = new MessageDialog(
        root,
        DialogFlags.Modal,
        result == string.Empty ? MessageType.Info : MessageType.Error,
        ButtonsType.Close,
        false,
        result == string.Empty ? "DEB file created in Documents." : result);

    dialog.Response += (s, a) => dialog.Destroy();
    dialog.ShowAll();

    stack.VisibleChild = content;
};

btnAbout.Clicked += (o, e) => stack.VisibleChild = about;
btnBackContent.Clicked += (o, e) => stack.VisibleChild = content;

btnSetDef.Clicked += (o, e) =>
{
    File.WriteAllLines(FastPath("data"), defdata);

    packageName.Text = defdata[0];
    applicationName.Text = defdata[1];
    version.Text = defdata[2];
    architecture.Active = 0;
    authors.Text = defdata[4];
    description.Text = defdata[5];
    folderPath.Text = defdata[6];
    iconPath.Text = defdata[7];
    installPath.Text = defdata[8];
    executable.Text = defdata[9];
    category.Active = 12;
};

content.Add(logo);
content.Add(applicationNameText);
content.Add(applicationName);
content.Add(packageNameText);
content.Add(packageName);
content.Add(authorText);
content.Add(authors);
content.Add(versionText);
content.Add(version);
content.Add(descriptionText);
content.Add(description);
content.Add(new Label());
content.Add(architectureText);
content.Add(architecture);
content.Add(folderText);
content.Add(folderPath);
content.Add(executableText);
content.Add(executable);
content.Add(iconPathText);
content.Add(iconPath);
content.Add(installPathText);
content.Add(installPath);
content.Add(categoryText);
content.Add(category);
content.Add(new Label());
content.Add(btnContainer);

building.Add(spinner);
building.PackStart(buildTextLoading, false, true, 0);

about.PackStart(logoAbout, false, true, 2);
about.PackStart(authorProgram, false, true, 0);
about.PackStart(versionAbout, false, true, 0);
about.PackStart(textAbout, true, true, 2);
about.PackStart(btnBackContent, false, true, 0);

root.Add(stack);
root.ShowAll();

Application.Run();

string FastPath(string file) => Path.Combine(AppContext.BaseDirectory, file);