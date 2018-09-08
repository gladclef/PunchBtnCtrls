# Punch Button Controls

A set of buttons to add meta-controls to my computer. These buttons are intended to be multi-functional yet easy to use because of their solid "punchable" design requiring little to no dexterity of the fat fingers.

## Pieces

1. C# code to capture screenshots of programs.
2. C# code to transfer images to Arduino over serial connection.
3. Arduino code to display the punch buttons on TFTLCD screens.

## Pushing Screenshots

Images are sent via serial COM port from the C# program to the Arduino, one row at a time. They are reduced in resolution to try and provide useful screenshots in the least amount of time possible, as the serial connection (and the Arduino) are both quite slow.

As images continue to remain static, the low-res images are replaced with hi-res images. This is a GIF illustrating that process:

![Demo GIF](https://github.com/gladclef/PunchBtnCtrls/blob/master/PunchBtnCtrls/resources/demos/PushImageSmall.GIF)
![Full Resolution](https://beanweb.us/me/projects/PunchBtnCtrls/PushImage.GIF "Pushing Screenshots Illustration")
