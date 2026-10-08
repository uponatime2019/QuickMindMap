# Contributing to QuickMindMap

Thank you for your interest in contributing to **QuickMindMap**! Whether you are fixing a bug, adding new features, improving documentation, or suggesting UX enhancements, your support is warmly welcomed.

---

## 💡 Ways to Contribute

- **Report Bugs**: Submit clear and detailed bug reports via GitHub Issues. Include your Windows version, steps to reproduce, and screenshots or logs if applicable.
- **Suggest Features**: Have ideas for new mind mapping capabilities, export formats, or UI improvements? Open a feature request issue!
- **Submit Pull Requests**: Implement bug fixes, new features, or code improvements.
- **Improve Documentation**: Enhance the README, guides, or code comments.

---

## 🛠️ Development Setup & Workflow

### Prerequisites
- **Windows 10 (version 1809 / build 17763 or higher)** or **Windows 11**
- **.NET 8.0 SDK** (or later)
- **Visual Studio 2022** (v17.8+) with the **.NET Desktop Development** workload and **Windows App SDK C# Templates**

### Getting Started

1. **Fork the Repository**:
   Fork the repository on GitHub and clone your fork locally:
   ```bash
   git clone https://github.com/<your-username>/QuickMindMap.git
   cd QuickMindMap
   ```

2. **Open the Solution**:
   - Double-click `QuickMindMap.sln` to open the solution in Visual Studio 2022, or open the project folder.

3. **Build the Project**:
   ```bash
   dotnet build QuickMindMap.csproj -p:Platform=x64
   ```

4. **Run the Application**:
   ```bash
   dotnet run --project QuickMindMap.csproj
   ```

---

## 🔄 Pull Request Guidelines

1. **Branch Naming**:
   Use descriptive branch names (e.g., `feature/custom-connectors`, `fix/export-crash`, `docs/readme-shortcuts`).

2. **Code Style**:
   - Follow standard C# and .NET coding conventions.
   - Keep classes modular and preserve unpackaged runtime compatibility (avoid hard dependencies on MSIX package identity).

3. **Testing**:
   - Ensure the project builds cleanly without errors:
     ```bash
     dotnet build QuickMindMap.csproj -p:Platform=x64
     ```
   - Verify self-contained Release publish runs properly:
     ```bash
     dotnet publish QuickMindMap.csproj -c Release -p:Platform=x64 -o publish_out
     ```

4. **Submit PR**:
   - Push your branch to your fork and submit a Pull Request targeting the `main` branch.
   - Describe what changed, why, and any screenshots for UI adjustments.

---

## 💬 Community & Code of Conduct

Please be respectful, constructive, and encouraging to fellow community members and maintainers. Thank you for helping make QuickMindMap better for everyone!
