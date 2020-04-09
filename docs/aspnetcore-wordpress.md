# ASP.NET Core with WordPress

*WpDotNet* is a package made primarily to be used as a part of an ASP.NET Core application. Its possible uses are:

- Run WordPress on ASP.NET Core, without PHP, without having source files on server.
- Add WordPress as frontend to an existing ASP.NET Core application.
- Combine WordPress and MVC or Razor views.
- Extend WordPress with plugins in C#.

## Create and deploy an ASP.NET Core Web application with WordPress

The following tutorial is shown on Visual Studio 2019.

1. In Visual Studio, create a new *ASP.NET Core Web Application*. Select **File | New | Project** and search for **ASP.NET Core Web Application** template for C#.

    ![New ASP.NET Core Application](img/new-aspnetcore-csharp.png)

    Continue with **Next** and **Create**.

2. In the last step, make sure you are targeting **ASP.NET Core**, at least **3.0**. Other options can customized. For an empty web site, choose **Empty** project template as it does not have any unnecessary logic.

    ![New ASP.NET Core Application](img/new-aspnetcore-step2.png)

    Continue with **Create**.

3. Add a package reference to **PeachPied.WordPress.AspNetCore**. Right click the project and select **Manage NuGet Packages...**. Switch to **Browse** tab, tick **Include prerelease**, and search for **PeachPied.WordPress.AspNetCore**.

    ![Browse NuGet](img/browse-nuget-peachpied-wordpress-aspnetcore.png)

    Click **Install** and wait for the process to complete. Accept the license terms if prompted.

4. Next add WordPress to your request pipeline. Navigate to class **Startup** in file **Startup.cs**.

    ![WordPress request pipeline](img/startup-class.png)

    Add `UseWordPress()` into your *IAplicationBuilder*, preferably right before *UseRouting()*.

    ```c#

        app.UseWordPress();

    ```

    The optional `services.AddWordPress( ... )` can be used to alter WordPress configuration such as Site URL, database connection credentials, and others described in [configuration](../configuration/).

5. Prepare MySQL server. Make sure you have MySQL server running:

    - MySQL server installed and running. It is up to you whether to install it locally, run it in a virtualized environment such as docker, or elsewhere.
    - A database is created. It can be an empty database or a duplicate of an existing database with WordPress.
    - Ensure you have valid username, password, and server address.

6. Enter MySQL connection credentials to **appsettings.json**. Open *appsettings.json* file and add following section:

    ![appsettings.json](img/appsettings.png)

    ```json
    {
    "WordPress": {
        "dbhost": "localhost",
        "dbpassword": "password",
        "dbuser": "root",
        "dbname": "wordpress"
    }
    }

    ```

7. Run the application. Hit `F5` or press **Start** button in Visual Studio.

8. Deploy the application using *Publish* wizard. Right click the project, select **Publish**, and follow the instructions.

    ![deploy the application to cloud](img/publish.png)

## Grab a sample solution

The complete ASP.NET Core application with WordPress is available on GitHub:

- https://github.com/iolevel/peachpie-wordpress

The content of the repository is ready to be built, debugged, and published. It can be opened in a .NET IDE (Visual Studio, VS Code, Rider) for further customization.

## Deploy Pre-Compiled to Azure

In case you don't need any customization, and you just want to publish the WordPress running on .NET on your Azure, follow the link below:

- https://azuredeploy.net/?repository=https://github.com/iolevel/azure-wpdotnet

> Please note, the deployment link above requires the target Azure cluster to have .NET Core 3.1 SDK installed.
