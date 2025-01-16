using Artemis.Plugins.Devices.iDotMatrix.Enums;
using RGB.NET.Core;
using System.Threading.Tasks;

namespace Artemis.Plugins.Devices.iDotMatrix
{
    public class iDotMatrixDevice : AbstractRGBDevice<iDotMatrixDeviceInfo>, ILedMatrix
    {
        protected iDotMatrixUpdateQueue updateQueue;
        public iDotMatrixDevice(iDotMatrixDeviceInfo deviceInfo, iDotMatrixUpdateQueue updateQueue) : base(deviceInfo, updateQueue)
        {
            this.updateQueue = updateQueue;
            InitializeLayout(updateQueue.GetModel());
        }

        private void InitializeLayout(iDotMatrixModel model)
        {
            int size = model == iDotMatrixModel.x16 ? 16 : 32;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    AddLed(LedId.LedMatrix1 + x + (y * 32), new Point(x*11.375f,y*11.375f),new Size(10.2f,10.2f));
                }
            }
        }
        public void TurnOff()
        {
            updateQueue.TurnOff();
        }
        public void TurnOn()
        {
            updateQueue.TurnOn();
        }
        public async Task CloseDeviceAsync()
        {
            await updateQueue.CloseDeviceAsync();
        }
        protected override object? GetLedCustomData(LedId ledId)
        {
            int id = ledId - LedId.LedMatrix1;
            return new Point(id % 32, id / 32);
        }
    }
}
