###ShiftSync – Scheduling System
## Overview
---
ShiftSync is a scheduling system designed to help students who balance university work, part-time jobs and personal commitments. It automatically generates a structured weekly plan by combining shifts, tasks and user preferences.

The aim of the system is to reduce manual planning and create more realistic schedules by preventing conflicts and accounting for factors such as fatigue and recovery time.
---
# Features
Add and manage work shifts
Add and prioritise tasks
Automatically generate a weekly schedule
Prevent overlapping time blocks
Enforce minimum sleep requirements
Highlight scheduling conflicts when tasks cannot be scheduled
Technologies Used
Blazor WebAssembly (Frontend)
C# (Application logic and scheduling engine)
SQLite (Data storage)
Entity Framework Core (Database management)
Visual Studio 2022 (Development environment)
---
## System Structure

The system is built using a modular structure with three main components:

User Interface (UI): Handles user interaction and displays the schedule
Scheduling Engine: Generates the weekly plan using constraints and scoring
Data Storage: Stores shifts, tasks and user preferences using SQLite
How to Run
Open the solution file (.sln) in Visual Studio 2022
Build the project
Run the application
Use the interface to add shifts and tasks
Generate a weekly schedule
Project Context

This project was developed as part of a final year Development Project for BSc Computer Science with Artificial Intelligence at Sheffield Hallam University.
---
## Notes
This is a single-user application
Data is stored locally using SQLite
The system focuses on realistic scheduling rather than optimisation complexity
---
