# Project Context

## About This Project

EshopModularMonoliths — a tutorial project following the Udemy course:
[NET Backend Bootcamp: Modulith, VSA, DDD, CQRS and Outbox](https://www.udemy.com/course/net-backend-bootcamp-modulith-vsa-ddd-cqrs-and-outbox/)

## Project Structure

```
EshopModularMonoliths/          ← repo root (git, .editorconfig, .gitignore, README.md, CLAUDE.md live here)
└── src/                        ← Visual Studio solution opened from here
    ├── eshop-modular-monoliths.sln
    ├── Bootstrapper/
    ├── Modules/
    └── Shared/
```

- The `.sln` file is inside `src/`, not at the repo root.
- Visual Studio is opened by pointing it at `src/`.
- Config files that apply repo-wide (`.editorconfig`, `.gitignore`) live at the repo root — EditorConfig traverses up the directory tree so this works correctly.

## About the Developer

- Expert frontend developer (React & Angular), learning .NET backend for the first time
- Goal: become a fullstack developer
- Comes from a VS Code background — not yet familiar with Visual Studio IDE
- Little knowledge of ASP.NET — explain .NET and backend concepts clearly, don't assume prior backend familiarity. 
- Do not have experience with OOP (object oriented programming) in practice, has therotical knowledge but can not understand OOP related stuff easily.
