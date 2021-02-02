
namespace WpfCommApp.Helpers
{
    public class DifferenceTracker
    {
        private ChannelTracker[] _tracker;


        public DifferenceTracker(int size)
        {
            _tracker = new ChannelTracker[size];
        }

        public void IncChannelPhaseDiffRegister(int channel, int phase)
        {
            IncChannelPhaseRegisterValue(channel, phase, 1);
        }

        public void IncChannelPhaseSameRegister(int channel, int phase)
        {
            IncChannelPhaseRegisterValue(channel, phase, 0);
        }

        public void ZeroChannelPhaseDiffRegister(int channel, int phase)
        {
            ZeroChannelPhaseRegisterValue(channel, phase, 1);
        }

        public void ZeroChannelPhaseSameRegister(int channel, int phase)
        {
            ZeroChannelPhaseRegisterValue(channel, phase, 0);
        }

        public int GetChannelPhaseDiffRegister(int channel, int phase)
        {
            return GetChannelPhaseRegisterValue(channel, phase, 1);
        }

        public int GetChannelPhaseSameRegister(int channel, int phase)
        {
            return GetChannelPhaseRegisterValue(channel, phase, 0);
        }

        private void IncChannelPhaseRegisterValue(int channel, int phase, int register)
        {
            _tracker[channel].IncPhaseRegisterValue(phase, register);
        }

        private void ZeroChannelPhaseRegisterValue(int channel, int phase, int register)
        {
            _tracker[channel].ZeroPhaseRegisterValue(phase, register);
        }

        private int GetChannelPhaseRegisterValue(int channel, int phase, int register)
        {
            return _tracker[channel].GetPhaseRegisterValue(phase, register);
        }
    }

    public class ChannelTracker
    {
        private PhaseTracker[] _tracker;

        public ChannelTracker()
        {
            _tracker = new PhaseTracker[2];
        }

        public void IncPhaseRegisterValue(int phase, int register)
        {
            _tracker[phase].IncRegisterValue(register);
        }

        public void ZeroPhaseRegisterValue(int phase, int register)
        {
            _tracker[phase].ZeroRegisterValue(register);
        }

        public int GetPhaseRegisterValue(int phase, int register)
        {
            return _tracker[phase].GetRegisterValue(register);
        }
    }

    public class PhaseTracker
    {
        private RegisterTracker[] _tracker;

        public PhaseTracker()
        {
            _tracker = new RegisterTracker[2];
        }

        public void IncRegisterValue(int register)
        {
            _tracker[register].IncValue();
        }

        public void ZeroRegisterValue(int register)
        {
            _tracker[register].ZeroValue();
        }

        public int GetRegisterValue(int register)
        {
            return _tracker[register].GetValue();
        }
    }

    public class RegisterTracker
    {
        private int _counter;

        public RegisterTracker()
        {
            _counter = 0;
        }

        public void IncValue()
        {
            _counter++;
        }

        public void ZeroValue()
        {
            _counter = 0;
        }

        public int GetValue()
        {
            return _counter;
        }
    }
}
