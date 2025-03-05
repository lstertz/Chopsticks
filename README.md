[![Native Tests](https://github.com/lstertz/Chopsticks/actions/workflows/run-native-tests.yml/badge.svg?event=pull_request)](https://github.com/lstertz/Chopsticks/actions/workflows/run-native-tests.yml?query=event%3Apull_request)
[![Unity Tests](https://github.com/lstertz/Chopsticks/actions/workflows/run-unity-tests.yml/badge.svg?event=pull_request)](https://github.com/lstertz/Chopsticks/actions/workflows/run-unity-tests.yml?query=event%3Apull_request)

# Chopsticks
A suite of Unity-first SDKs, each referred to as a Chopstick, to support rapid iteration and scalable development of all types of applications.

## Chopstick Goals
- Independent, but Co-Supportive - Any Chopstick can be used on its own, but just like real chopsticks, they can do so much more when used together.
- Unity-First, but not Unity-Only - Chopsticks are designed for effective use within Unity, but they can be used in non-Unity projects or in independent C# code (like an SDK) that may later be used by Unity projects.
- Plug-and-Play Setup - Each Chopstick can be integrated into any Unity project with minimal setup.
- Minimal Changes for Integration - Bringing a Chopstick into an established project can be done iteratively, with very few changes per use case.
- Leverages the Unity Editor - Each Chopstick works with the Unity Editor, to the extent necessary to streamline the workflow of Unity devs and designers.
- Performant At-Scale - Chopsticks add minimal overhead to runtime performance, with acceptable and configurable tradeoffs for startup performance.
- Optional Advanced Configuration - Chopsticks are highly configurable, including through extension, for large scale and enterprise projects.
