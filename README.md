# Junction Switch Replacer

A Derail Valley mod that allows you to replace junction switch models with custom ones.

## Description

Junction Switch Replacer is a Unity Mod Manager mod for Derail Valley that provides the ability to replace the default junction switch models with custom 3D models.

## Features

- Replace junction switch models with custom Assetfile
- Support for restoring original switch models
- Hot-reloading of custom models
- Automatic switch detection and processing

## Requirements

- Derail Valley
- Unity Mod Manager

## Installation

1. Install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) if you haven't already
2. Download the latest release from the [Releases](../../releases) page
3. Install via Unity Mod Manager

## Add your own Model

1. Open the `JunctionSwitchReplacer.Unity` Folder with `Unity 2019.4.40f1`.
2. Add a new Folder and place your model and textures into it
3. Create a material and apply it to your model
4. Create a new AssetBundle tag in the Inspector window (bottom right, AssetBundle dropdown)
5. Name your AssetBundle tag (e.g., "customswitch")
6. Select your model and material, then assign the AssetBundle tag to both
7. Go to `Assets > Build AssetBundles` to export your custom model
8. Copy the generated AssetBundle file to your Derail Valley mods folder


### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Fuggschen**

## Support

If you encounter any issues or have suggestions, please open an issue on the [GitHub repository](../../issues).
