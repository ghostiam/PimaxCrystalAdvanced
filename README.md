## Tobii Eye Tracking Module for VRCFaceTracking

A module for working with Tobii Eye Tracking in VRCFaceTracking.\
Tested on [Pimax Crystal](https://pimax.com/crystal/?ref=ghostiam).

### Usage

- Download and install [VRCFaceTracking](https://github.com/benaclejames/VRCFaceTracking)
- Download the archive with the module from [release](https://github.com/ghostiam/VRCFT-Tobii-Advanced/releases/latest)
- Run VRCFaceTracking
- Go to the "Module Registry" tab
- Click "+ Install Module From .zip"
- Select the downloaded archive
- Done!

### How to use the "advanced" mode.

> [!IMPORTANT]
> You must have a file with a valid license, otherwise the module will not work in "advanced" mode!\
> Or you can use the workaround: [Broken Eye](https://github.com/ghostiam/BrokenEye) (Pimax Crystal only)

By default, the module is loaded without a license, which allows you to use it
to track combined gaze direction and eye opening/closing.

The "Advanced" mode allows you to track the gaze direction for
each eye separately, as well as get information about the pupil diameter.

For this, you must have a valid license, in which is indicated

```
"featureGroup": "professional"
```

(you can see an example in the file [license.example.json](license.example.json))

It needs to be placed in the module folder

```
C:\Users\<USERNAME>\AppData\Roaming\VRCFaceTracking\CustomLibs\324b3cd5-5e64-4f3f-b056-12340badc0de

# Or paste into the address bar of the explorer to open the folder
%APPDATA%\VRCFaceTracking\CustomLibs\324b3cd5-5e64-4f3f-b056-12340badc0de
```

under the name `license.json`

After launching VRCFaceTracking, make sure that the module has loaded your license, it should be in the "Output" tab:

```
...
[TobiiTrackingModule] Information: Loading license...
[TobiiTrackingModule] Information: Creating device with license.
[TobiiTrackingModule] Information: Connected to platform module with build version "<version>"
[TobiiTrackingModule] Information: Subscribe to advanced data.
...
```

If instead you see:

```
[TobiiTrackingModule] Information: No license found in <redacted>
```

It means the license was not found, check the path and file name.

And if you see:

```
...
[TobiiTrackingModule] Warning: License validation failed: TOBII_LICENSE_VALIDATION_RESULT_TAMPERED
...
```

It means you are using an invalid license, check its content.

### How to get a license for "advanced" mode.

![I don't know](https://www.meme-arsenal.com/memes/087bd8289778ed9f50fb7f4ec1e23dab.jpg)

Most likely you can only wait until the manufacturer of the device you are using gets a license from Tobii.

### How to use the "advanced" mode without a license.

There is a workaround that will allow you to obtain data on the direction of gaze for each eye separately, the diameter
of the pupil, and even receive images from cameras! And all this without a “professional” license!

The software is called [Broken Eye](https://github.com/ghostiam/BrokenEye) and can be downloaded 
[here](https://github.com/ghostiam/BrokenEye/releases/latest) (currently only works for Pimax Crystal).