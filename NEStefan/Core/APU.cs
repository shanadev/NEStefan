using System;
using Microsoft.Win32;

namespace NEStefan.Core;


public struct Sequencer
{
    public int sequence = 0b00000000;
    public int newsequence = 0b00000000;
    public ushort timer = 0x0000;
    public ushort reload = 0x0000;
    public byte output = 0x00;

    public Sequencer() { }

    public byte clock(bool enable, Func<int,int> funcManip)
    {
        if (enable) 
        {
            timer--;
            if (timer == 0xFFFF)
            {
                timer = (ushort)(reload + 1);
                sequence = funcManip(sequence);
                output = (byte)(sequence & 0b00000001); 
            }
        }
        //Console.WriteLine(output);
        return output;
    }
}


public struct DMCChannel
{
    public bool irqEnable = false;
    public bool loop = false;
    public byte frequency = 0;
    public ushort loadCounter = 0;
    public ushort SampleAddr = 0x0000;
    public ushort SampleLength = 0x0000;

    public bool interuptFlag = false;
    public int rate = 0;


    public ushort output_level = 0; // this gets incremented or decremented over time

    public ushort SampleBuffer = 0x0000;

    public ushort CurrSampleAddr = 0x0000;
    public ushort bytesRemaining = 0x0000;

    public DMCChannel() { }

    public void clock()
    {
        // memory reader reads into the buffer if the buffer is empty

        // 
    }

    public float sample()
    {
        // get the next byte from the sample buffer

        // increment or decrement the output
        return 0f;
    }


}


public struct noiseChannel
{
    public ushort timer = 0x0000;
    public ushort shiftRegister = 1;
    //public byte mode = 0;
    public int phase = 0;
    public bool loopnoise = false;
    public ushort reload = 0;

    public noiseChannel() {
        //shiftRegister = 0xDBDB;
        shiftRegister = 1;
    }

    public void clock()
    {
        //Feedback is calculated as the exclusive - OR of bit 0 and one other bit: bit 6 if Mode flag is set, otherwise bit 1.
        //The shift register is shifted right by one bit.
        //Bit 14, the leftmost bit, is set to the feedback calculated earlier.
        ushort feedback = 0;
        if (loopnoise)
        {
            feedback = (byte)((shiftRegister & 0b1) ^ ((shiftRegister & 0b1000000) >> 6));
        }
        else
        {
            feedback = (byte)((shiftRegister & 0b1) ^ ((shiftRegister & 0b10) >> 1));
        }
        feedback = (ushort)(feedback << 14);
        shiftRegister = (ushort)(shiftRegister >> 1);
        //shiftRegister &= 0b0111_1111_1111_1111;
        shiftRegister &= 0x7FFF;
        shiftRegister = (ushort)(shiftRegister | feedback);


    }


    public float sample(float t)
    {
        //return 0.0f;

        return shiftRegister & 0b01;

        //long rnd = Random.Shared.NextInt64(-1, 1);
        //return (float)rnd;
    }
}

public struct oscpulse
{
    public float frequency = 0f;
    public float dutycycle = 0f;
    public float amplitude = 0.8f;
    public float harmonics = 20f;

    public oscpulse() { }

    public float sample(float t)
    {
        
        //amplitude = .8f;
        float a = 0;
        float b = 0;
        float p = (float)(dutycycle * 2.0f * Math.PI);

        for (int n = 1; n < harmonics; n++)
        {
            float c = (float)(n * frequency * 2.0f * Math.PI * t);
            a += (float)(-APU.approxSin(c) / n);
            b += (float)(-APU.approxSin(c - p * n) / n);
        }

        return (float)(2.0f * amplitude / Math.PI) * (a - b);
        //return (float)(.5f * Math.Sin(2.0 * Math.PI * frequency * t));
    }
}

public struct trianglechannel
{
    public ushort timer = 0;
    public ushort new_timer = 0;

    public ushort reload = 0;
    public ushort new_reload = 0;

    public ushort linear_timer = 0;
    public ushort new_linear_timer = 0;

    public bool reload_linear = false;
    public bool new_reload_linear = false;

    public float frequency = 0f;
    public float new_frequency = 0f;

    //public int sampleIndex = 0;

    public ushort linear_reload_amount = 0;
    public ushort new_linear_reload_amount = 0;

    public byte ControlFlag = 0;

    public bool haltFlag = false;

    private float lastoutput = 0;

    private int phasetimer = 0;
    private int phaselength = 0;

    public trianglechannel() { }

    public void clocktimer()
    {
        //sampleIndex++;
        if (!haltFlag)
        {
            timer--;
            if (timer == 0xFFFF)
            {
                timer = (ushort)(reload + 1);

            }
        }
        //phaselength = (int)Math.Floor(44100 / frequency);

    }

    public void clocklinear()
    {
        if (haltFlag)
        {
            linear_timer = linear_reload_amount;
        }
        else if (linear_timer > 0)
        {
            linear_timer--;

        }

        if (ControlFlag == 0) haltFlag = false;

        if (reload_linear)
        {
            linear_timer = (ushort)(linear_reload_amount + 1);
            reload_linear = false;
        }

    }

    private void ProcessNewValues()
    {
        timer = new_timer;
        reload = new_reload;
        frequency = new_frequency;
        linear_reload_amount = new_linear_reload_amount;
        linear_timer = new_linear_timer;
        reload_linear = new_reload_linear;
        phaselength = (int)Math.Floor( 44100 / frequency );
    }


    public float sample(float t)
    {
        phasetimer++;

        if (phasetimer >= phaselength)
        {
            // process new values
            ProcessNewValues();
            // reset timer
            phasetimer = 0;
        }


        float p = 44100 / (frequency);
        float output;
        //float volume = 1f;
        //float output = (4 / p) * Math.Abs(( ( ((t - (p/4)) % p) + p) % p ) - (p/2)) - 1;

        //float output = Math.Abs((t % p));
        //output = ((4 * audioState.toneVolume) / wavePeriod) * Math.Abs(((audioState.sampleIndex - wavePeriod / 4) % wavePeriod) - (wavePeriod / 2)) - audioState.toneVolume;
        //output = ((4 * volume) / p) * Math.Abs(((t - p / 4) % p) - (p / 2)) - volume;

        //output = ((4 * volume) / p) * Math.Abs(((t - p / 4) % p) - (p / 2)) - volume - 1;
        if (linear_timer > 0)
        {

            //output = APU.approxSin((float)(2.0 * Math.PI * t * frequency));

            //output = 2 * Math.Abs(2 * ((t / p) - (float)Math.Floor((t / p) + (1 / 2)))) - 1;
            //output = Math.Abs((t % p));

            //float centersection = APU.approxSin((float)(((2 * Math.PI) / p) * t));

            output = (float)((2 / Math.PI) * Math.Asin(APU.approxSin((float)(((2 * Math.PI) / p) * (t*44100)))));
            if (float.IsNaN(output))
            {
                output = lastoutput;
                //Console.WriteLine("naan");
            }
            if (output > 1 || output < -1)
            {
                output = lastoutput;
                //Console.WriteLine("Out of bounds");
            }
        }
        else
        {
            output = 0.0f;
        }
        //Console.WriteLine(output);
        //return (float)(Math.Abs(1 - (-1)) / p * (p - Math.Abs((t % (2 * p)) - p)) + (-1));



        lastoutput = output;
        return output;
    }
}

public struct lengthcounter
{
    public lengthcounter() { }

    public ushort counter = 0x00;
    public ushort clock(bool enable, bool halt)
    {
        if (!enable)
        {
            counter = 0;
        }
        else
        {
            if (counter > 0 && !halt) counter--;
        }
        return counter;
    }
}

public struct envelope
{
    public bool start = false;
    public bool disable = false;
    public int divider_count = 0;
    public int volume = 0;
    public int output = 0;
    public int decay_count = 0;

    public envelope() { }

    public void clock(bool loop)
    {
        if (!start)
        {
            if (divider_count == 0)
            {
                divider_count = volume;
                if (decay_count == 0)
                {
                    if (loop)
                    {
                        decay_count = 15;
                    }
                }
                else decay_count--;
            }
            else divider_count--;
        }
        else
        {
            start = false;
            decay_count = 15;
            divider_count = volume;
        }

        if (disable)
        {
            output = volume;
        }
        else
        {
            output = decay_count;
        }
    }
}

public struct sweeper
{
    public bool enabled = false;
    public bool down = false;
    public bool reload = false;
    public byte shift = 0x00;
    public byte timer = 0x00;
    public byte period = 0x00;
    public ushort change = 0;
    public bool mute = false;

    public sweeper() { }

    public void track(ref ushort target)
    {
        if (enabled)
        {
            change = (ushort)(target >> shift);
            mute = (target < 8) || (target > 0x7FFF);
        }
    }

    public bool clock(ref ushort target, bool channel)
    {
        bool changed = false;
        if (timer == 0 && enabled && shift > 0 && !mute)
        {
            if (target >= 8 && change < 0x07FF)
            {
                if (down)
                {
                    target -= (ushort)(change - (channel ? 1 : 0));
                }
                else
                {
                    target += change;
                }
                changed = true;
            }
        }

        {
            if (timer == 0 || reload)
            {
                timer = period;
                reload = false;

            }
            else
            {
                timer--;
            }
            mute = (target < 8) || (target > 0x7FF);
        }
        return changed;
    }
}


public class APU
{
    public bool Pulse1UserEnable = true;
    public bool Pulse2UserEnable = true;
    public bool TriangleUserEnable = true;
    public bool NoiseUserEnable = true;

    public static float approxSin(float t)
    {
        float j = t * 0.15915f;
        j = j - (int)j;
        return 20.785f * j * (j - 0.5f) * (j - 1.0f);
    }

    public Queue<float> pulse1buffer = new Queue<float>();
    public Queue<float> pulse2buffer = new Queue<float>();
    public Queue<float> trianglebuffer = new Queue<float>();
    public Queue<float> noisebuffer = new Queue<float>();

    private Bus _bus;

    public Sequencer pulse1Seq;
    public oscpulse pulse1Osc = new oscpulse();
    public envelope pulse1Env;
    public lengthcounter pulse1LC;
    public sweeper pulse1Sweep;
    private bool _pulse1Enable = false;
    private float _pulse1Sample = 0.0f;
    private bool _pulse1halt = false;
    double pulse1_sample = 0.0;
    double pulse1_output = 0.0;

    public Sequencer pulse2Seq;
    public oscpulse pulse2Osc = new oscpulse();
    public envelope pulse2Env;
    public lengthcounter pulse2LC;
    public sweeper pulse2Sweep;
    private bool _pulse2Enable = false;
    private float _pulse2Sample = 0.0f;
    private bool _pulse2halt = false;
    double pulse2_sample = 0.0;
    double pulse2_output = 0.0;


    public trianglechannel triangleChan;
    public lengthcounter triangleLC;
    private bool _triangleEnable = false;
    private float _triangleSample = 0.0f;
    private bool _trianglehalt = false;
    double triangle_sample = 0.0;
    double triangle_output = 0.0;


    public noiseChannel noiseChan = new noiseChannel();
    public lengthcounter noiseLC;
    public envelope noiseEnv;
    public float _noiseSample = 0.0f;
    public bool _noiseHalt = false;
    public bool _noiseEnable = false;
    double noise_sample = 0.0;
    double noise_output = 0.0;

    public DMCChannel DMCChan = new DMCChannel();
    public bool _dmcEnable = false;

    public double dGlobalTime = 0.0;
    public uint iGlobalTime = 0;


    public uint frame_clock_counter = 0; // maintain musical timing.
    public uint clock_counter = 0;

	public APU(Bus nesBus)
	{
        _bus = nesBus;
	}

    public float GetOutputSample()
    {
        if (!Pulse1UserEnable) pulse1_output = 0.0;
        if (!Pulse2UserEnable) pulse2_output = 0.0;
        if (!TriangleUserEnable) triangle_output = 0.0;
        if (!NoiseUserEnable) noise_output = 0.0;

        pulse1buffer.Enqueue((float)pulse1_output);
        pulse2buffer.Enqueue((float)pulse2_output);
        trianglebuffer.Enqueue((float)triangle_output);
        noisebuffer.Enqueue((float)noise_output);

        //return _pulse1Sample;
        double pulseout = 95.88 / ((8128 / (pulse1_output + pulse2_output)) + 100);
        double tndout = 159.79 / ((1 / ((triangle_output / 8227) + (noise_output / 12241))) + 100);

        //return (float)(((1.0 * pulse1_output) - 0.8) * 0.1 +
        //    ((1.0 * pulse2_output) - 0.8) * 0.1 +
        //    ((1.0 * triangle_output) - 0.8) * 0.1 +
        //    ((1.0 * noise_output) - 0.8) * 0.1);
        return (float)(pulseout + tndout) * 10;
    }

    public byte[] length_table = {  10, 254, 20,  2, 40,  4, 80,  6,
                                    160,   8, 60, 10, 14, 12, 26, 14,
                                     12,  16, 24, 18, 48, 20, 96, 22,
                                    192,  24, 72, 26, 16, 28, 32, 30 };

    public ushort[] noise_timer_period_lookup = { 4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068 };

    public int[] dmc_rate_table = { 428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54 };


    public void cpuWrite(ushort addr, byte data)
	{
		switch (addr)
		{
			//Pulse 1
			case 0x4000:
                // DDLC VVVV	Duty (D), envelope loop / length counter halt (L), constant volume (C), volume/envelope (V)
                switch ((data & 0xC0) >> 6)
                {
                    case 0x00: pulse1Seq.newsequence = 0b00000001; pulse1Osc.dutycycle = .125f; break;
                    case 0x01: pulse1Seq.newsequence = 0b00000011; pulse1Osc.dutycycle = .250f; break;
                    case 0x02: pulse1Seq.newsequence = 0b00001111; pulse1Osc.dutycycle = .500f; break;
                    case 0x03: pulse1Seq.newsequence = 0b11111100; pulse1Osc.dutycycle = .750f; break;

                }
                pulse1Seq.sequence = pulse1Seq.newsequence;
                _pulse1halt = (data & 0x20) > 0 ? true : false;
                pulse1Env.volume = (int)(data & 0x0F);
                pulse1Env.disable = (data & 0x10) > 0 ? true: false;

                //pulse1Osc.amplitude = data & 0b00001111;
                break;

			case 0x4001:
                pulse1Sweep.enabled = (data & 0x80) > 0 ? true : false;
                pulse1Sweep.period = (byte)((data & 0x70) >> 4);
                pulse1Sweep.down = (data & 0x08) > 0 ? true : false ;
                pulse1Sweep.shift = (byte)(data & 0x07);
                pulse1Sweep.reload = true;
                //EPPP NSSS	Sweep unit: enabled (E), period (P), negate (N), shift (S)

                break;
            case 0x4002:
                // TTTT TTTT   Timer low(T)
                pulse1Seq.reload = (ushort)((pulse1Seq.reload & 0xFF00) | data);
                break;
            case 0x4003:
                // 	LLLL LTTT	Length counter load (L), timer high (T)


                pulse1Seq.reload = (ushort)(((data & 0x07)) << 8 | (pulse1Seq.reload & 0x00FF));
                pulse1Seq.timer = pulse1Seq.reload;
                pulse1Seq.sequence = pulse1Seq.newsequence;
                pulse1LC.counter = length_table[(data & 0xF8) >> 3];
                pulse1Env.start = true;
                break;

            //Pulse 2
            case 0x4004:
                // DDLC VVVV	Duty (D), envelope loop / length counter halt (L), constant volume (C), volume/envelope (V)
                switch ((data & 0xC0) >> 6)
                {
                    case 0x00: pulse2Seq.newsequence = 0b00000001; pulse2Osc.dutycycle = .125f; break;
                    case 0x01: pulse2Seq.newsequence = 0b00000011; pulse2Osc.dutycycle = .250f; break;
                    case 0x02: pulse2Seq.newsequence = 0b00001111; pulse2Osc.dutycycle = .500f; break;
                    case 0x03: pulse2Seq.newsequence = 0b11111100; pulse2Osc.dutycycle = .750f; break;

                }
                pulse2Seq.sequence = pulse2Seq.newsequence;
                _pulse2halt = (data & 0x20) > 0 ? true : false;
                pulse2Env.volume = (int)(data & 0x0F);
                pulse2Env.disable = (data & 0x10) > 0 ? true : false;
                break;
            case 0x4005:
                //EPPP NSSS	Sweep unit: enabled (E), period (P), negate (N), shift (S)
                pulse2Sweep.enabled = (data & 0x80) > 0 ? true : false;
                pulse2Sweep.period = (byte)((data & 0x70) >> 4);
                pulse2Sweep.down = (data & 0x08) > 0 ? true : false;
                pulse2Sweep.shift = (byte)(data & 0x07);
                pulse2Sweep.reload = true;
                break;
            case 0x4006:
                // TTTT TTTT   Timer low(T)
                pulse2Seq.reload = (ushort)((pulse2Seq.reload & 0xFF00) | data);
                break;
            case 0x4007:
                // 	LLLL LTTT	Length counter load (L), timer high (T)
                pulse2Seq.reload = (ushort)(((data & 0x07)) << 8 | (pulse2Seq.reload & 0x00FF));
                pulse2Seq.timer = pulse2Seq.reload;
                pulse2Seq.sequence = pulse2Seq.newsequence;
                pulse2LC.counter = length_table[(data & 0xF8) >> 3];
                pulse2Env.start = true;
                break;

            //Triangle
            case 0x4008:
                //CRRR RRRR   Length counter halt / linear counter control(C), linear counter load(R)
                //_trianglehalt = (data & 0x80) > 0 ? true : false;
                triangleChan.haltFlag = (data & 0x80) > 0 ? true : false;
                //triangleChan. = (ushort)(data & 0x7F);
                triangleChan.new_linear_timer = (ushort)(data & 0x7F);

                triangleChan.new_reload_linear = false;
                triangleChan.new_linear_reload_amount = (ushort)(data & 0x7F);
                //triangleLC.counter = triangleChan.reload;
                break;
            case 0x4009:
                // unused
                break;
            case 0x400A:
                //TTTT TTTT	Timer low (T)
                triangleChan.new_reload = (ushort)((triangleChan.new_reload & 0xFF00) | data);
                break;
            case 0x400B:
                //LLLL LTTT	Length counter load (L), timer high (T)
                // sets linear counter reload flag for triangle channel
                triangleChan.new_reload = (ushort)(((data & 0x07)) << 8 | (triangleChan.new_reload & 0x00FF));
                triangleChan.new_reload_linear = true;
                triangleLC.counter = length_table[(data & 0xF8) >> 3];
                break;


            //Noise
            case 0x400C:
                _noiseHalt = (data & 0x20) > 0 ? true : false;
                noiseEnv.volume = (data & 0x0F);
                noiseEnv.disable = (data & 0x10) > 0 ? true: false ;
                break;
            case 0x400D:
                break;
            case 0x400E:
                noiseChan.reload = noise_timer_period_lookup[(data & 0x0F)];
                noiseChan.loopnoise = (data & 0xF0) > 0 ? true : false;
                break;
            case 0x400F:
                pulse1Env.start = true;
                pulse2Env.start = true;
                //noiseChan.reload = (ushort)(data & 0xFFF0);
                noiseEnv.start = true;
                noiseLC.counter = length_table[(data & 0xF8) >> 3];
                break;

            //DMC
            case 0x4010:
                // IL--.RRRR
                DMCChan.irqEnable = (data & 0b1000_0000) > 0 ? true : false;
                DMCChan.loop = (data & 0b0100_0000) > 0 ? true : false;
                DMCChan.frequency = (byte)(data & 0b0000_1111);
                DMCChan.rate = dmc_rate_table[DMCChan.frequency];
                break;
            case 0x4011:
                DMCChan.loadCounter = (ushort)(data & 0b0111_1111);
                DMCChan.output_level = DMCChan.loadCounter;
                break;
            case 0x4012:
                DMCChan.SampleAddr = (ushort)(0xC000 + (data * 64));
                break;
            case 0x4013:
                DMCChan.SampleLength = (ushort)(data * 16 + 1);
                break;

            // Enable
            case 0x4015:
                _pulse1Enable = (data & 0b00000001) > 0;
                _pulse2Enable = (data & 0b00000010) > 0;
                _triangleEnable = (data & 0b00000100) > 0;
                if (!_triangleEnable) triangleLC.counter = 0;
                _noiseEnable = (data & 0b00001000) > 0;
                _dmcEnable = (data & 0b00010000) > 0;
                break;

            // Frame counter
            case 0x4017:
                break;
        }
	}

	public byte cpuRead(ushort addr)
	{
		byte output = 0x00;

		return output;
	}

	public void Clock()
	{
        // The clock for APU runs at half the rate of the CPU clock
        // The CPU clock runs at a third of the rate of the PPU clock

        bool bQuarterFrameClock = false;
        bool bHalfFrameClock = false;

        float addval = (0.3333333333f / 1789773.0f);
        //uint wtf = (uint)(addval) * 1000;
        //iGlobalTime += wtf;
        //dGlobalTime += (0.3333333333f / 1789773f);
        dGlobalTime = dGlobalTime + addval;

        if (clock_counter % 6 == 0)
        {
            frame_clock_counter++;

            // 4-step sequence mode
            if (frame_clock_counter == 3729)
            {
                bQuarterFrameClock = true;
            }
            if (frame_clock_counter == 7457)
            {
                bQuarterFrameClock = true;
                bHalfFrameClock = true;
            }
            if (frame_clock_counter == 11186)
            {
                bQuarterFrameClock = true;
            }
            if (frame_clock_counter == 14916)
            {
                bQuarterFrameClock = true;
                bHalfFrameClock = true;
                frame_clock_counter = 0;
            }

            if (bQuarterFrameClock)
            {
                // adjust volume envelope
                pulse1Env.clock(_pulse1halt);
                pulse2Env.clock(_pulse2halt);
                triangleChan.clocklinear();
                noiseEnv.clock(_noiseHalt);
            }

            if (bHalfFrameClock)
            {
                pulse1LC.clock(_pulse1Enable, _pulse1halt);
                pulse1Sweep.clock(ref pulse1Seq.reload, false);
                pulse2LC.clock(_pulse2Enable, _pulse2halt);
                pulse2Sweep.clock(ref pulse2Seq.reload, false);
                triangleLC.clock(_triangleEnable, _trianglehalt);
                noiseLC.clock(_noiseEnable, _noiseHalt);
            }

            pulse1Seq.clock(_pulse1Enable, (s) =>
            {
                return ((s & 0x0001) << 7) | ((s & 0x00FE) >> 1);
            });

            //_pulse1Sample = pulse1Seq.output;

            //double test = 1789773.0 / (16 * (pulse1Seq.reload + 1));

            pulse1Osc.frequency = (float)(1789773.0f / (16.0f * (float)(pulse1Seq.reload + 1)));
            pulse1Osc.amplitude = (float)(pulse1Env.output - 1) / 16.0f;
            _pulse1Sample = pulse1Osc.sample((float)dGlobalTime);

            //if (pulse1Osc.frequency < 12.4 || pulse1Osc.frequency > 54.6) _pulse1Sample = 0.0f;


            //_pulse1Sample = pulse1Osc.sample((float)(iGlobalTime / 1000));
            //Console.WriteLine(_pulse1Sample);

            if (pulse1LC.counter > 0 && pulse1Seq.timer >= 8 && !pulse1Sweep.mute && pulse1Env.output > 2)
            {
                //pulse1_output += (_pulse1Sample - pulse1_output) * 0.5;
                pulse1_output = _pulse1Sample;
            }
            else
            {
                pulse1_output = 0;
            }

            if (!_pulse1Enable) pulse1_output = 0;


            pulse2Seq.clock(_pulse2Enable, (s) =>
            {
                return ((s & 0x0001) << 7) | ((s & 0x00FE) >> 1);
            });

            //_pulse1Sample = pulse1Seq.output;

            //double test = 1789773.0 / (16 * (pulse1Seq.reload + 1));

            pulse2Osc.frequency = (float)(1789773.0f / (16.0f * (float)(pulse2Seq.reload + 1)));
            pulse2Osc.amplitude = (float)(pulse2Env.output - 1) / 16.0f;
            _pulse2Sample = pulse2Osc.sample((float)dGlobalTime);

            //if (pulse2Osc.frequency < 12.4 || pulse2Osc.frequency > 54.6) _pulse2Sample = 0.0f;


            //_pulse1Sample = pulse1Osc.sample((float)(iGlobalTime / 1000));
            //Console.WriteLine(_pulse1Sample);

            if (pulse2LC.counter > 0 && pulse2Seq.timer >= 8 && !pulse2Sweep.mute && pulse2Env.output > 2)
            {
                //pulse1_output += (_pulse1Sample - pulse1_output) * 0.5;
                pulse2_output = _pulse2Sample;
            }
            else
            {
                pulse2_output = 0;
            }

            if (!_pulse2Enable) pulse2_output = 0;


            triangleChan.clocktimer();

            //f = fCPU/(32*(tval + 1))
            triangleChan.new_frequency = (float)(1789773.0f / (32.0f * (triangleChan.new_reload + 1)));
            //triangleChan.frequency = triangleChan.frequency / 36;
            _triangleSample = triangleChan.sample((float)dGlobalTime);

            //if (triangleChan.frequency < 27.3 || triangleChan.frequency > 55.9) _triangleSample = 0.0f;

            if (triangleChan.linear_timer > 0 && triangleLC.counter >= 8)
            {
                triangle_output = _triangleSample;

            }
            else
            {
                triangle_output = 0;
            }


            noiseChan.clock();

            _noiseSample = noiseChan.sample((float)dGlobalTime);
            //noise_output = _noiseSample;

            //if (_noiseSample > 0)
            //{
            //    Console.WriteLine("df");
            //}

            if ((noiseChan.shiftRegister & 0x01) != 0 && noiseLC.counter >= 8)
            {
                noise_output = _noiseSample * (float)(noiseEnv.output - 1) / 16.0f;
            }
            else
            {
                noise_output = 0;
            }


        }
        
        pulse1Sweep.track(ref pulse1Seq.reload);
        pulse2Sweep.track(ref pulse2Seq.reload);



        clock_counter++;
	}

	public void Reset()
	{

	}
}

