# Overview
A helper program to extract image path and other information from file system and insert into a database for the image repository project.

##Requirements

This project uses [ASP.NET Core User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) for storing sensitive configuration data during development.

To set up your development environment:

1. Clone the repository.
2. Open the project in Visual Studio.
3. Right-click on the project and select **Manage User Secrets**.
4. Add the following secrets to your `secrets.json` file:

    ```json
    {
      "ConnectionStrings:ImageDb": "Your connection string here",
    }
    ```

5. Modify the `Development` launch profile environmental variable `NEVCAN_DIRECTORY_ROOT_PATH` to the root directory that contains your images.
6. Save the program and run in Development. 
