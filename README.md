# U2NetSharp

> **Status**: Under Development 🚧  
> U2NetSharp is a library that leverages the U2Net model to remove image backgrounds.

## 📚 About the Project

U2NetSharp simplifies the automatic removal of image backgrounds using the U2Net model. Additionally, the project includes a WebSocket server (`U2NetSharp.Server`) that allows clients to send images and receive processed images with the background removed.

This repository also contains a small frontend for testing the WebSocket service. The frontend connects to the WebSocket server on port `8075` and facilitates image uploads for background removal.

## 🚀 Features

### **Library Usage**
- The library can be instantiated directly to remove the background of an image passed to it.

### **WebSocket Server**
- The `U2NetSharp.Server` project initializes a WebSocket server on port `8075`, enabling clients to send images to the service for background removal.

### **Frontend for Testing**
- The frontend, located at [`frontend`](https://github.com/felipefranzim/u2netsharp/tree/main/frontend), provides an interface to test the WebSocket service.

## 🛠️ Tech Stack

- **Primary Language**: C#
- **Other Technologies**: 
  - HTML
  - JavaScript

## 📦 Installation and Execution

### **1. Build the Project**

To build the `U2NetSharp.Server` project, follow these steps:

1. Navigate to the project directory:
   ```bash
   cd src/U2NetSharp.Server
   ```

2. Run the build command:
   ```bash
   dotnet build
   ```

3. The executable will be generated at:
   ```
   bin\Debug\netX.X\U2NetSharp.Server.exe
   ```

### **2. Run the Server**

#### **Using `dotnet run`**
If you want to run the server directly, use the following command:

1. Navigate to the project directory:
   ```bash
   cd src/U2NetSharp.Server
   ```

2. Start the server:
   ```bash
   dotnet run
   ```

#### **Install as a Windows Service**
If you wish to install and run the server as a Windows service:

1. Build the project as described above.
2. Use the `sc.exe` utility to register the executable as a service:
   ```bash
   sc create U2NetSharpServer binPath= "C:\Path\To\bin\Debug\netX.X\U2NetSharp.Server.exe" start= auto
   ```

3. Start the service:
   ```bash
   sc start U2NetSharpServer
   ```

4. To stop the service:
   ```bash
   sc stop U2NetSharpServer
   ```

5. To remove the service:
   ```bash
   sc delete U2NetSharpServer
   ```

---

## 🖥️ Frontend Integration

The frontend is located in the [`frontend`](https://github.com/felipefranzim/u2netsharp/tree/main/frontend) folder and includes:

1. **`U2NetSharpConnector.js`**: A JavaScript class that handles the WebSocket connection to the server on port `8075`. It includes methods to:
   - Connect to the WebSocket server.
   - Send image data in Base64 format.
   - Receive processed images from the server.

2. **`index.html`**: A simple HTML page for testing the service. It allows users to:
   - Select an image file.
   - Preview the selected image.
   - Send the image to the WebSocket server for background removal.
   - Display the processed image returned by the server.

### **Frontend Workflow**

Here’s how the integration works:

1. The `U2NetSharpConnector` class is instantiated and used to establish a WebSocket connection (`ws://localhost:8075/background-removal`).
2. When an image is selected via the file input, it is converted to a Base64 string using a `FileReader`.
3. The Base64 string is sent to the server using the `sendMessage` method of the `U2NetSharpConnector` class.
4. The server processes the image and sends back the result as a Base64 string.
5. The processed image is displayed in the browser.

### **Usage Example**

1. Open the `index.html` file in a browser.
2. Select an image file using the file input.
3. The image will be sent to the WebSocket server, and the processed image will be displayed once the server responds.

---

## 🤝 Contributing

Contributions are always welcome! To contribute:

1. Fork the project.
2. Create a branch for your feature/fix:
   ```bash
   git checkout -b my-feature
   ```

3. Commit your changes:
   ```bash
   git commit -m "Add my new feature"
   ```

4. Push to the remote repository:
   ```bash
   git push origin my-feature
   ```

5. Open a Pull Request.

---

## 📄 License

This project does not yet have a specified license. Consider adding a license to the repository to define the usage and distribution of the code.

## 📬 Contact

- **Author**: [Felipe Franzim](https://github.com/felipefranzim)
- **Repository**: [GitHub - U2NetSharp](https://github.com/felipefranzim/u2netsharp)

---

<p align="center">Made with ❤️ by Felipe Franzim</p>