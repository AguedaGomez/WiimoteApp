using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiimoteLib;

namespace GestureLib
{
    public class GestureCapturer
    {
        public event Action<Gesture> GestureCaptured;

        public void OnWiimoteChanged(WiimoteState state)
        {
            bool buttonBpressed = state.ButtonState.B;
            Point3F sample = state.AccelState.Values;

            if (buttonBpressedOld)
            {
                if (buttonBpressed)
                    gesture.AddSample(new double[] { sample.X, sample.Y, sample.Z });
                else if (GestureCaptured != null)
                    GestureCaptured(gesture);
            }
            else
            {
                if (buttonBpressed)
                    gesture = new Gesture();
                else
                    ; //Nada
            }
            buttonBpressedOld = buttonBpressed;
        }

        #region private

        bool buttonBpressedOld;
        Gesture gesture;

        #endregion
    }
}
