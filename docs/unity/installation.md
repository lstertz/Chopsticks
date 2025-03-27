# Installation

Chopsticks Dependencies is most easily installed as a package from the Git repo. Steps to do so are outlined by Unity: [https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-giturl.html](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-giturl.html)

The Git URL to use for the latest version is: [https://github.com/lstertz/Chopsticks.git#com.chopsticks.dependencies](https://github.com/lstertz/Chopsticks.git#com.chopsticks.dependencies)

A version number can be appended to the URL to use (and lock to) that specific version of the package, like so: [https://github.com/lstertz/Chopsticks.git#com.chopsticks.dependencies-1.0.0](https://github.com/lstertz/Chopsticks.git#com.chopsticks.dependencies)

#### Using the Samples

Package samples are included in the `~Samples` folder. These are excluded from any project that imports the package but can be viewed within the project. To make use of the Samples in any way, the desired contents within `~Samples` must be copied to the project's `Assets` folder.

Unfortunately, any references within the copied scenes will try to point to their original content within the `~Samples` folder, so all scripts and other references (such as materials) must be re-assigned (or re-created) using references within the project.
