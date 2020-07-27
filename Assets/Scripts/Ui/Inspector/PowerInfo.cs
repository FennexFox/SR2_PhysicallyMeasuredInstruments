namespace Assets.Scripts.Ui.Inspector
{
    using System;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Input;
    using ModApi.Ui.Inspector;

    public class PowerInfo
    {
        private IInputController _input;

        private IFuelSource _battery;

        private float _inputVolt;

        private float _resistance;

        private float _maxAmpere;

        public float InputAmpere => Math.Abs(_input.Value * _maxAmpere);

        public float PowerConsumption => _inputVolt * InputAmpere / 1000f;

        public void AddPowerInfo(PartInspectorModel model)
        {
            var powerInfo = new GroupModel("Power Info"); // need to add ElectroMagnet Info
            powerInfo.Add(new TextModel("Input Volt", () => GetPowerInfoString("InpVolt")));
            powerInfo.Add(new TextModel("Internal Volt", () => GetPowerInfoString("IntVolt")));
            powerInfo.Add(new TextModel("Ampere", () => GetPowerInfoString("Ampere")));
            powerInfo.Add(new TextModel("Watt", () => GetPowerInfoString("Watt")));
            model.AddGroup(powerInfo);
        }

        public string GetPowerInfoString(string Label)
        {
            string result = null;
            switch (Label)
            {
                case "InpVolt":
                    result = $"{_inputVolt} V"; break;
                case "IntVolt":
                    result = $"{InputAmpere * _resistance:n0} V"; break;
                case "Ampere":
                    result = $"{InputAmpere:n0} A"; break;
                case "Watt":
                    result = $"{PowerConsumption * 1000f:n0} W"; break;
            }
            return result;
        }

        public void ConsumePower(float DeltaTime, IPartScript part)
        {
            if (!_battery.IsEmpty) { _battery.RemoveFuel(PowerConsumption * DeltaTime); }
            else { part.Data.Activated = false; }
        }

        public void UpdateBattery(IFuelSource Battery)
        {
            _battery = Battery;
        }

        public PowerInfo(IInputController inputController, IFuelSource Battery, float InputVolt, float MaxAmpere, float Resistance)
        {
            _input = inputController;
            _battery = Battery;
            _inputVolt = InputVolt;
            _maxAmpere = MaxAmpere;
            _resistance = Resistance;
        }
    }
}