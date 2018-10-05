<!-- BEGIN TOC GLOBAL -->
<!-- END TOC GLOBAL -->

# About NUKE

[<img align="right" width="350px" src="https://github.com/nuke-build/all/raw/master/images/logo-black.png" />](https://nuke.build)

Started in April 2017, NUKE is a free, open-source **build automation system for C#/.NET**. It runs on **any platform** (.NET Core, .NET Framework, Mono) and **integrates natively** with all IDEs (VisualStudio, JetBrains Rider, etc.). While builds are **bootstrapped with conventional scripts** (_build.ps1_ or _build.sh_), their actual implementation is based on simple **C# console applications**. This allows IDE features like code-completion, refactorings and debugging to be used as usual. One further step is that **build steps are actual symbols** (expression-bodied properties) and therefore provide superior navigation and type-safety.<!-- NUKE supports a variety of CLI tools commonly used in .NET  Utilizing **code-generation for CLI tool support** allows NUKE to powerful, flexible and consistent API for--> 

<!--[<img width="600px" src="https://github.com/nuke-build/all/raw/master/images/features.gif" />](#)-->

- _New to NUKE? Get started by either [reading](https://www.nuke.build/getting-started.html) or [watching](https://www.youtube.com/watch?v=7gEqxzD6hbs)._
- _Continue further? Become a [stargazer](https://github.com/nuke-build/nuke/stargazers) and check our [documentation](https://www.nuke.build/api/Nuke.Common/Nuke.Common.ControlFlow.html)._
- _Love to socialize and chat? Join us on [Slack](https://slofile.com/slack/nukebuildnet), [Gitter](https://gitter.im/nuke-build/nuke) or [Twitter](https://twitter.com/nukebuildnet)._
- _Want to help us? Read our [contribution guidelines](#)._

## Project leads

[<img align="left" width="75px" src="https://github.com/nuke-build/all/raw/master/images/matkoch.png" />](https://twitter.com/matkoch87)
**Matthias Koch** &mdash; Lover of clean code, API design and software architecture. He started with NUKE in 2017 after searching for the perfect build system for one of his open-source projects. At JetBrains he is working as a developer advocate for the .NET department. [Follow him on Twitter!](https://twitter.com/matkoch87)

[<img align="left" width="75px" src="https://github.com/nuke-build/all/raw/master/images/arodus.jpg" />](https://twitter.com/s_karasek)
**Sebastian Karasek** &mdash; He is a passionate full stack developer familiar with different technologies and languages. Following his ideals to _automate everything_, he joined NUKE in early 2018 with special focus on code-generation for third-party tools. [Follow him on Twitter!](https://twitter.com/s_karasek)

# Repositories

## Common

<!-- BEGIN COMMON -->
| Name | Description |
| --- | --- |
<!-- END COMMON -->

## Extensions

<!-- BEGIN EXTENSIONS -->
| Name | Description |
| --- | --- |
<!-- END EXTENSIONS -->

## Addons

<!-- BEGIN ADDONS -->
| Name | Description |
| --- | --- |
| nuke-build/docfx | This NUKE addon provides a <a href="http://www.nuke.build/getting-started.html#clt-wrappers">CLI wrapper</a> for <a href="https://dotnet.github.io/docfx">DocFX</a>:<br /><br /><blockquote>DocFX is an API documentation generator for .NET, and currently it supports C# and VB. It generates API reference documentation from triple-slash comments in your source code. It also allows you to use Markdown files to create additional topics such as tutorials and how-tos, and to customize the generated reference documentation. DocFX builds a static HTML website from your source code and Markdown files, which can be easily hosted on any web servers (for example, <em>github.io</em>). Also, DocFX provides you the flexibility to customize the layout and style of your website through templates. If you are interested in creating your own website with your own styles, you can follow <a href="http://dotnet.github.io/docfx/tutorial/howto_create_custom_template.html">how to create custom template</a> to create custom templates.</blockquote> |
| nuke-build/nswag | This NUKE addon provides a <a href="http://www.nuke.build/getting-started.html#clt-wrappers">CLI wrapper</a> for <a href="https://github.com/RSuter/NSwag">NSwag</a>:<br /><br /><blockquote>The project combines the functionality of Swashbuckle (Swagger generation) and AutoRest (client generation) in one toolchain. This way a lot of incompatibilites can be avoided and features which are not well described by the Swagger specification or JSON Schema are better supported (e.g. <a href="https://github.com/NJsonSchema/NJsonSchema/wiki/Inheritance">inheritance</a>, <a href="https://github.com/NJsonSchema/NJsonSchema/wiki/Enums">enum</a> and reference handling). The NSwag project heavily uses <a href="http://njsonschema.org">NJsonSchema for .NET</a> for JSON Schema handling and C#/TypeScript class/interface generation.</blockquote> |
<!-- END ADDONS -->
