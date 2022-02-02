# anvil-unity-dots

An opinionated collection of systems and utilities that add [Unity](https://unity.com) [ECS](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/index.html) [DOTS](https://unity.com/dots) specific implementations of [Anvil](https://github.com/decline-cookies/anvil-csharp-core) systems and add new common tools and systems that are uniquely useful to Unity ECS/DOTS development.

Refer to the [anvil-csharp-core](https://github.com/decline-cookies/anvil-csharp-core) for a description of Anvil's purpose and the team's motivations.
Refer to the [anvil-unity-core](https://github.com/decline-cookies/anvil-unity-core) for the Object Oriented aspect of Anvil within Unity.

## Expectations
See: [anvil-csharp-core](https://github.com/decline-cookies/anvil-csharp-core)

The code is reasonably clean but documentation and examples are sparse. Feel free to [reach out on Twitter](https://twitter.com/declinecookies) or open issues with questions.

⚠️ We welcome PRs and bug reports but making this repo a public success is not our priority. No promises on when it will be addressed!

# Dependencies
- [Unity](https://unity.com/)
- [anvil-csharp-core](https://github.com/decline-cookies/anvil-csharp-core)
- [anvil-unity-core](https://github.com/decline-cookies/anvil-unity-core)
- *ECS and DOTS Packages*
    - [com.unity.burst](https://docs.unity3d.com/Packages/com.unity.burst@1.6/manual/index.html)
    - [com.unity.collections](https://docs.unity3d.com/Packages/com.unity.collections@0.15/manual/index.html)
    - [com.unity.entities](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/index.html)
    - [com.unity.jobs](https://docs.unity3d.com/Packages/com.unity.jobs@0.8/manual/index.html)
    - [com.unity.mathematics](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/manual/index.html)
    
At this point in time, all ECS and DOTS related functionality will go into this one submodule. As it grows and it makes sense to, we may split the functionality into further submodules. Splitting to a separate submodule per ECS/DOTS Unity package seems overly tedious at the moment.

# Features
 - [ ] TODO: [#1](https://github.com/decline-cookies/anvil-ecs-dots-core/issues/1)

# Project Setup
1. Add [Dependencies](#dependencies) as submodules or Unity Packages to your project
2. Make use of [Features](#features) as desired.
3. Done!

This is the recommended Unity project folder structure:
- Assets
  - Anvil
    - csharp
      - anvil-csharp-core
    - unity
      - anvil-unity-core
      - anvil-ecs-dots-core
