# Task View
This package contains an In-Editor task view for Unity.

## Installation
1. Open "Package Manager"
2. Choose "Add package from git URL..."
3. Use the HTTPS URL of this repository:
```
https://github.com/yanicksenn/com.yanicksenn.taskview.git#1.0.0
```
4. Click "Add"

## Features
This package provides a simple yet powerful task management tool directly within the Unity Editor:
* **Task Management:** Create, edit, and organize tasks without leaving Unity.
* **Multiple Lists:** Support for multiple task lists (ScriptableObjects) to organize tasks by category or milestone.
* **Object Linking:** Link relevant Unity assets or scene objects directly to a task for quick access.
* **Progress Tracking:** Mark tasks as completed and track creation/update times.

## How to Use

### Opening the Task View
To open the task view window, go to the Unity menu and select `Window > Task View`.

### Managing Task Lists
* **Create New List:** Click the "New List" button in the toolbar to create a new `TaskListAsset` in your project.
* **Switch Lists:** Use the dropdown menu in the toolbar to switch between available task lists.
* **Default List:** If no list exists, a default one can be created via the "Create Default Task List" button.

### Managing Tasks
* **Add Task:** Click the "+" button at the bottom of the task list (left panel).
* **Edit Task:** Select a task from the list to view and edit its details in the right panel. You can modify the title, description, and add links.
* **Complete Task:** Specific tasks can be marked as completed using the checkbox in the list or the details view.
* **Reorder:** Tasks can be reordered by dragging them in the list.
