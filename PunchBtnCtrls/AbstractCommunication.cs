using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsSnapshots
{
    /// <summary>
    /// For sending images to, and recieving lines of test from, the microcontroller devices.
    /// </summary>
    public abstract class AbstractCommunication
    {
        public delegate void ReceivedLineDelegate(string line);
        public delegate void StatusChangedDelegate();

        /// <summary>True when connected.</summary>
        public bool isConnected { get; protected set; } = false;
        /// <summary>Called when there is a successful connection established.</summary>
        public StatusChangedDelegate ConnectedCallback = null;
        /// <summary>Called when the connection ends.</summary>
        public StatusChangedDelegate DisconnectedCallback = null;
        /// <summary>Called when a new line of text data is recieved from the device.</summary>
        public ReceivedLineDelegate ReceivedLineCallback = null;
        /// <summary>Width of the screen to be drawn to.</summary>
        public uint screenWidth;

        /// <summary>
        /// Registers the <see cref="ConnectedCallback"/>.
        /// </summary>
        public AbstractCommunication(uint screenWidth, StatusChangedDelegate ConnectedCallback = null)
        {
            this.screenWidth = screenWidth;
            if (ConnectedCallback != null)
                this.ConnectedCallback += ConnectedCallback;
        }

        /// <summary>
        /// Tries to connect to the assigned port.
        /// Calls <see cref="ConnectedCallback"/> on success.
        /// </summary>
        /// <exception cref="IOException">If connecting fails.</exception>
        public abstract void Connect();

        /// <summary>
        /// Disconnects from the assigned port.
        /// Calls <see cref="DisconnectedCallback"/> when no longer connected.
        /// </summary>
        /// <exception cref="IOException">If disconnecting fails.</exception>
        public abstract void Disconnect();

        /// <summary>
        /// Tries to send an image to the 
        /// </summary>
        /// <param name="row">The row data to draw.</param>
        /// <param name="startRow">The starting y-index of the row (top).</param>
        /// <param name="rowSpan">The number of vertical pixels to draw this same row over (aka inverse of img resolution).</param>
        /// <returns>True on success, false on failure. If false, then the device is most likely disconnected.</returns>
        public abstract bool SendImageRow(int[] row, uint startRow, uint rowSpan);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="includeNewline"></param>
        /// <returns>True on success, false on failure. If false, then the device is most likely disconnected.</returns>
        public abstract bool SendString(string data, bool includeNewline = false);

        /// <summary>
        /// Creates a left-to-right gradient image to send to the device.
        /// </summary>
        /// <param name="colSpan">The number of horizontal pixels to draw each row over (aka inverse of img resolution).</param>
        /// <param name="rowSpan">The number of vertical pixels to draw each row over (aka inverse of img resolution).</param>
        public abstract void SendTestImage(uint colSpan, uint rowSpan);
    }
}
