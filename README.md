# anvil-unity-dots

An opinionated collection of systems and utilities that add [Unity](https://unity.com) [DOTS](https://unity.com/dots) specific implementations on top of Anvil.

Refer to [anvil-csharp-core](https://github.com/decline-cookies/anvil-csharp-core) and [anvil-unity-core](https://github.com/decline-cookies/anvil-unity-core) for a description of Anvil's purpose, the team's motivations, and the features provided to traditional Unity development.

## Expectations
See: [anvil-csharp-core](https://github.com/decline-cookies/anvil-csharp-core)

The code is reasonably clean but documentation and examples are sparse. Feel free to [reach out on Twitter](https://twitter.com/declinecookies) or open issues with questions.

⚠️ We welcome PRs and bug reports but making this repo a public success is not our priority. No promises on when it will be addressed!

# Dependencies
- [Unity (2021.1.9)](https://unity.com/)
- [anvil-csharp-core (main...usually 😬)](https://github.com/decline-cookies/anvil-csharp-core)
- [anvil-unity-core (main...usually 😬)](https://github.com/decline-cookies/anvil-unity-core)
- *DOTS Packages*
    - [com.unity.burst (1.6.4)](https://docs.unity3d.com/Packages/com.unity.burst@1.6/manual/index.html)
    - [com.unity.collections (0.15.0-preview21)](https://docs.unity3d.com/Packages/com.unity.collections@0.15/manual/index.html)
    - [com.unity.entities (0.17.0-preview42)](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/index.html)
    - [com.unity.jobs (0.8.0-preview23)](https://docs.unity3d.com/Packages/com.unity.jobs@0.8/manual/index.html)
    - [com.unity.mathematics (1.2.5)](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/manual/index.html)
    
At this point in time, all DOTS related functionality is in this one repository. In the future, we may split the functionality into specialized repositories. Maintaining a repository per Unity DOTS package seems overly tedious at the moment.

# Features
 - [ ] TODO: [#1](https://github.com/decline-cookies/anvil-unity-dots/issues/1)

# Project Setup
1. Add [dependencies](#dependencies) as submodules or Unity Packages to your project
2. Make use of [features](#features) as desired.
3. Done!

This is the recommended Unity project folder structure:
- Assets
  - Anvil
    - csharp
      - anvil-csharp-core
    - unity
      - anvil-unity-core
      - anvil-unity-dots
