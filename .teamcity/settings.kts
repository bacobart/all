import jetbrains.buildServer.configs.kotlin.v2018_1.*
import jetbrains.buildServer.configs.kotlin.v2018_1.buildFeatures.commitStatusPublisher
import jetbrains.buildServer.configs.kotlin.v2018_1.buildSteps.powerShell
import jetbrains.buildServer.configs.kotlin.v2018_1.triggers.vcs
import jetbrains.buildServer.configs.kotlin.v2018_1.vcs.GitVcsRoot

version = "2018.2"

project {
    description = "https://github.com/nuke-build"

    // BEGIN PROJECT
    vcsRoot(NukeBuildNukeVcsRoot)
    buildType(NukeBuildNukeBuildType)
    vcsRoot(NukeBuildAzureVcsRoot)
    buildType(NukeBuildAzureBuildType)
    vcsRoot(NukeBuildAzureKeyvaultVcsRoot)
    buildType(NukeBuildAzureKeyvaultBuildType)
    vcsRoot(NukeBuildCompressionVcsRoot)
    buildType(NukeBuildCompressionBuildType)
    vcsRoot(NukeBuildDocfxVcsRoot)
    buildType(NukeBuildDocfxBuildType)
    vcsRoot(NukeBuildDockerVcsRoot)
    buildType(NukeBuildDockerBuildType)
    vcsRoot(NukeBuildHelmVcsRoot)
    buildType(NukeBuildHelmBuildType)
    vcsRoot(NukeBuildKubernetesVcsRoot)
    buildType(NukeBuildKubernetesBuildType)
    vcsRoot(NukeBuildNswagVcsRoot)
    buildType(NukeBuildNswagBuildType)
    vcsRoot(NukeBuildPresentationVcsRoot)
    buildType(NukeBuildPresentationBuildType)
    vcsRoot(NukeBuildResharperVcsRoot)
    buildType(NukeBuildResharperBuildType)
    vcsRoot(NukeBuildWebVcsRoot)
    buildType(NukeBuildWebBuildType)
    vcsRoot(NukeBuildVscodeVcsRoot)
    buildType(NukeBuildVscodeBuildType)
    // END PROJECT

    features {
        feature {
            id = "PROJECT_EXT_343"
            type = "ReportTab"
            param("startPage", "coverage.zip!index.htm")
            param("title", "Code Coverage")
            param("type", "BuildReportTab")
        }
    }
}

open class CustomBuildType(configurationName: String, vcsRoot: GitVcsRoot) : BuildType() {
    init {
        name = configurationName
        description = vcsRoot.url!!

        artifactRules = "./output"

        vcs {
            root(vcsRoot)
        }

        steps {
            powerShell {
                scriptMode = file {
                    path = "build.ps1"
                }
                noProfile = false
            }
        }

        triggers {
            vcs {
                triggerRules = "+:**"
                branchFilter = ""
            }
        }

        //features {
        //    commitStatusPublisher {
        //        publisher = github {
        //            githubUrl = "https://api.github.com"
        //            authType = personalToken {
        //                token = "zxxa1c58b9d53d9223168b7b82f0705cb079d00f74adb1bfa306e19338b11d18c5570fb8c0d87e1be54775d03cbe80d301b"
        //            }
        //        }
        //    }
        //}
    }
}

open class CustomGitVcsRoot(url: String, defaultBranch: String) : GitVcsRoot() {
    init {
        name = "$url#refs/heads/$defaultBranch"
        this.url = url
        branch = "refs/heads/$defaultBranch"
        pollInterval = 60
        branchSpec = "+:refs/heads/*"
    }
}

// BEGIN OBJECTS
object NukeBuildNukeVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/nuke", "develop")
object NukeBuildNukeBuildType : CustomBuildType("nuke-build/nuke", NukeBuildNukeVcsRoot)
object NukeBuildAzureVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/azure", "master")
object NukeBuildAzureBuildType : CustomBuildType("nuke-build/azure", NukeBuildAzureVcsRoot)
object NukeBuildAzureKeyvaultVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/azure-keyvault", "master")
object NukeBuildAzureKeyvaultBuildType : CustomBuildType("nuke-build/azure-keyvault", NukeBuildAzureKeyvaultVcsRoot)
object NukeBuildCompressionVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/compression", "master")
object NukeBuildCompressionBuildType : CustomBuildType("nuke-build/compression", NukeBuildCompressionVcsRoot)
object NukeBuildDocfxVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/docfx", "master")
object NukeBuildDocfxBuildType : CustomBuildType("nuke-build/docfx", NukeBuildDocfxVcsRoot)
object NukeBuildDockerVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/docker", "master")
object NukeBuildDockerBuildType : CustomBuildType("nuke-build/docker", NukeBuildDockerVcsRoot)
object NukeBuildHelmVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/helm", "master")
object NukeBuildHelmBuildType : CustomBuildType("nuke-build/helm", NukeBuildHelmVcsRoot)
object NukeBuildKubernetesVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/kubernetes", "master")
object NukeBuildKubernetesBuildType : CustomBuildType("nuke-build/kubernetes", NukeBuildKubernetesVcsRoot)
object NukeBuildNswagVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/nswag", "master")
object NukeBuildNswagBuildType : CustomBuildType("nuke-build/nswag", NukeBuildNswagVcsRoot)
object NukeBuildPresentationVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/presentation", "master")
object NukeBuildPresentationBuildType : CustomBuildType("nuke-build/presentation", NukeBuildPresentationVcsRoot)
object NukeBuildResharperVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/resharper", "master")
object NukeBuildResharperBuildType : CustomBuildType("nuke-build/resharper", NukeBuildResharperVcsRoot)
object NukeBuildWebVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/web", "master")
object NukeBuildWebBuildType : CustomBuildType("nuke-build/web", NukeBuildWebVcsRoot)
object NukeBuildVscodeVcsRoot : CustomGitVcsRoot("https://github.com/nuke-build/vscode", "master")
object NukeBuildVscodeBuildType : CustomBuildType("nuke-build/vscode", NukeBuildVscodeVcsRoot)
// END OBJECTS
