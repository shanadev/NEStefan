using System;
using System.Net.NetworkInformation;
using GameEngine;
using Serilog;

namespace NEStefan.Core
{
    // This is going to represent the Nintendo-
    // it has a CPU, PPU, Cart, APU, RAM, etc...
    public class Bus
    {
        // for disassembling
        // I need to get refreshed prg info after bank changes - which will be writes to mappers
        // just refreshig 0x8000 -> 0xFFFF
        public Dictionary<ushort, string> asmPRG = new Dictionary<ushort, string>();

        // devices on the bus
        public NESSystem nes;
        public CPU cpu;
        public PPU ppu;
        public byte[] cpuRam = new byte[2048];
        public Cartridge cart;
        public byte[] controller = new byte[2];
        public APU apu;

        public float dAudioSample = 0.0f;
        public float dAudioTimePerSystemSample = 0.0f;
        public float dAudioTimePerNESClock = 0.0f;
        public float dAudioTime = 0.0f;

        public uint samplesCount = 0;

        public byte dma_addr_start = 0;

        // engine inance passed from the main program
        // This is just being passed along to the PPU honestly
        //private Engine engine;

        private byte[] controller_state = new byte[2];  // the latched controller state that will be serially read
        
        private bool controllerStrobe1 = false;  // is the controller being polled?
        private bool controllerStrobe2 = false;

        private uint systemClockCounter = 0; // Master counter
        // TODO: make public accessible

        // Handling DMA
        byte dma_page = 0x00;   // page we're accessing, top byte
        byte dma_addr = 0x00;   // byte within the page, bottom byte
        byte dma_data = 0x00;   // the actual data being transferred

        bool dma_transfer = false; // is it happening?
        bool dma_wait = true;  // are we waiting to start DMA?

        public void SetSampleFrequency(int sampleRate)
        {
            dAudioTimePerSystemSample = 1.0f / (float)sampleRate;
            dAudioTimePerNESClock = 1.0f / 5369318.0f;
        }

        // Constructor - engine comes in, set up Ram, CPU and PPU
        public Bus(NESSystem n)
        {
            nes = n;
            //this.engine = engine;
            cpuRam = Enumerable.Repeat<byte>(0x00, cpuRam.Length).ToArray();
            cpu = new CPU(this);
            ppu = new PPU(this);
            apu = new APU(this);
        }

        // Main CPU Write method
        // Let eh cart/mapper have a crack first, then check each of the address regions to direct
        public void cpuWrite(ushort addr, byte data)
        {
            //Log.Debug($"CPU Write - addr:0x{Convert.ToString(addr, toBase:16).PadLeft(4,'0')} - data:0x{Convert.ToString(data, toBase: 16).PadLeft(2, '0')}");
            if (cart.cpuWrite(addr, data))  // give cart first crack at the write
            {
                // since there was a picked up write, I'm assuming a PRG bank change, so
                //if (addr >= 0x8000)
                //{
                //    asmPRG = cpu.Disassemble(0x8000, 0xFFFF);
                //    foreach (var pair in asmPRG)
                //    {
                //        nes.asm[pair.Key] = pair.Value;
                //    }
                //}
            }
            else if (addr >= 0x0000 && addr <= 0x1FFF) // write to ram - 2k mirrored to 8k
            {
                cpuRam[addr & 0x07FF] = data;
            }
            else if (addr >= 0x2000 && addr <= 0x3FFF)  // THis is a write to the PPU
            {
                ppu.cpuWrite((ushort)(addr & 0x0007), data);
            }
            else if ((addr >= 0x4000 && addr<= 0x4013) || addr == 0x4015 || addr == 0x4017)
            {
                apu.cpuWrite(addr, data);
            }
            else if (addr == 0x4014)
            {
                dma_page = data;
                dma_addr = cpuRead(0x2003, false);
                dma_addr_start = dma_addr;
                dma_transfer = true;
            }
            else if (addr == 0x4016)  // Write to the controller addresses - sets the state
            {
                if (controllerStrobe1 && !((data & 0x01) > 0))
                {
                    controller_state[0] = controller[0];
                    //controller_state[addr & 0x0001] = 0x01;
                }

                controllerStrobe1 = ((data & 0x01) > 0);
                //Log.Debug($"Write Controller - Strobe: ({(controllerStrobe ? "Y" : "N")}) - state: {Convert.ToString(controller_state[addr & 0x0001], toBase: 2).PadLeft(8, '0')} - data: {Convert.ToString(data, toBase: 2).PadLeft(8, '0')} ");


                if (controllerStrobe2 && !((data & 0x01) > 0))
                {
                    controller_state[1] = controller[1];
                    //controller_state[addr & 0x0001] = 0x01;
                }

                controllerStrobe2 = ((data & 0x01) > 0);
            }
            else if (addr == 0x4017)  // Write to t1he controller addresses - sets the state
            {
                if (controllerStrobe2 && !((data & 0x01) > 0))
                {
                    controller_state[1] = controller[1];
                    //controller_state[addr & 0x0001] = 0x01;
                }

                controllerStrobe2 = ((data & 0x01) > 0);
                //Log.Debug($"Write Controller - Strobe: ({(controllerStrobe ? "Y" : "N")}) - state: {Convert.ToString(controller_state[addr & 0x0001], toBase: 2).PadLeft(8, '0')} - data: {Convert.ToString(data, toBase: 2).PadLeft(8, '0')} ");

            }
        }

        // Main CPU Read method
        // Same here, let the cart/mapper look first to consume, then pass to the rest
        public byte cpuRead(int addr, bool readOnly = false)    // Readonly is for shutting off actual read during disassembly
        {
            byte data = 0x00;
            //Log.Debug($"CPU Read - addr:0x{Convert.ToString(addr, toBase: 16).PadLeft(4, '0')}");

            if (cart.cpuRead(addr, out data))
            {
                //return data;
                //Log.Debug($"Mapped Read Data = [{CPU.Hex(data, 2)}]");
            }
            else if (addr >= 0x0000 && addr <= 0x1FFF) // Read from ram - 2k mirrored
            {
                data = cpuRam[addr & 0x07FF];
            }
            else if (addr >= 0x2000 && addr <= 0x3FFF)  // Read from PPU
            {
                data = ppu.cpuRead((ushort)(addr & 0x0007), readOnly);
            }
            else if (addr == 0x4016)      // Read from controller - 8 reads in a row to get controller status
            {
                //string thing = "";
                if (controllerStrobe1)
                {
                    data = (byte)(((controller_state[0] & 0x40) > 0) ? 1 : 0);
                }
                else
                {
                    //thing = Convert.ToString(controller_state[addr & 0x0001], toBase: 2).PadLeft(8, '0');

                    data = (byte)(((controller_state[0] & 0x01) > 0) ? 1 : 0);

                    controller_state[0] >>= 1;

                }
                //Log.Debug($"Read Controller - Strobe: ({(controllerStrobe ? "Y" : "N")}) - state: {thing} - data: {Convert.ToString(data, toBase: 16).PadLeft(2, '0')} ");
            }
            else if (addr == 0x4017)      // Read from controller - 8 reads in a row to get controller status
            {
                //string thing = "";
                if (controllerStrobe2)
                {
                    data = (byte)(((controller_state[1] & 0x40) > 0) ? 1 : 0);
                }
                else
                {
                    //thing = Convert.ToString(controller_state[addr & 0x0001], toBase: 2).PadLeft(8, '0');

                    data = (byte)(((controller_state[1] & 0x01) > 0) ? 1 : 0);

                    controller_state[1] >>= 1;

                }
                //Log.Debug($"Read Controller - Strobe: ({(controllerStrobe ? "Y" : "N")}) - state: {thing} - data: {Convert.ToString(data, toBase: 16).PadLeft(2, '0')} ");
            }
            return data;

        }


        //// System methods

        // Set up new cartridge, connect to the PPU
        public void InsertCartridge(Cartridge cart)
        {
            this.cart = cart;
            ppu.ConnectCartridge(this.cart);

        }

        // Reset the system
        public void Reset()
        {
            cart.Reset();
            cpu.Reset(); 
            systemClockCounter = 0;

            dma_page = 0x00;
            dma_addr = 0x00;
            dma_data = 0x00;
            dma_wait = true;
            dma_transfer = false;
        }

        // Main NES Clock
        // The PPU is 3 times faster clocked than the CPU, so this runs at PPU speed
        public bool Clock()
        {
            ppu.Clock();
            //Console.WriteLine($"Bus: system counter - {systemClockCounter}");
            apu.Clock();

            if (systemClockCounter % 3 == 0)
            {
                if (dma_transfer)
                {
                    if (dma_wait)
                    {
                        if (systemClockCounter % 2 == 1)
                        {
                            dma_wait = false;
                        }
                    }
                    else
                    {
                        if (systemClockCounter % 2 == 0)
                         {
                            dma_data = cpuRead(dma_page << 8 | dma_addr);
                        }
                        else
                        {
                            cpuWrite(0x2004, dma_data);
                            //ppu.OAM[dma_addr] = dma_data;
                            dma_addr++;

                            if (dma_addr == dma_addr_start)
                            {
                                dma_transfer = false;
                                dma_wait = true;
                            }
                        }
                    }
                }
                else
                {
                    cpu.Clock();
                    // if debugging

                }
            }

            bool bAudioSampleReady = false;
            dAudioTime += dAudioTimePerNESClock;
            if (dAudioTime >= dAudioTimePerSystemSample)
            {
                dAudioTime -= dAudioTimePerSystemSample;
                dAudioSample = apu.GetOutputSample();
                //float newValue = (float)(Math.Sin((float)(2.0f * Math.PI * (samplesCount / 44100f) * 220f)));
                //samplesCount++;
                //dAudioSample = newValue;
                //Console.WriteLine(dAudioSample);
                bAudioSampleReady = true;
            }

            // Non-maskable interrupt - could happen any clock cycle and can't be stopped
            if (ppu.nmi)
            {
                ppu.nmi = false;
                cpu.NMI();
            }

            // check if cartridge is requesting IRQ
            if (cart.mapper.irqState())
            {
                cart.mapper.irqClear();
                cpu.IRQ();
            }

            systemClockCounter++; // increment the main counter

            return bAudioSampleReady;

        }

    }
}

