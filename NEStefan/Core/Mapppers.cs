using System;
using System.Xml;
using Serilog;

namespace NEStefan.Core
{
    // enum for the type of mirroring - sometimes hard-wired on the game chip
    public enum MIRROR
    {
        HORIZONTAL,
        VERTICAL,
        HARDWARE,
        ONESCREEN_LO,
        ONESCREEN_HI,
        FOURSCREEN
    }

    // abstract Mapper class
    public abstract class Mapper
    {
        protected byte PRGbanks = 0;
        protected byte CHRbanks = 0;
        protected MIRROR mirrorMode;

        public List<byte> RAMStatic = new List<byte>();


        public Mapper(byte prgBanks, byte chrBanks)
        {
            PRGbanks = prgBanks;
            CHRbanks = chrBanks;
            reset();
        }

        public virtual bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            mapped_addr = addr;
            data = 0x00;
            return false;
        }

        public virtual bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
        {
            mapped_addr = addr;
            return false;

        }

        public virtual bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            mapped_addr = addr;
            return false;
        }

        public virtual bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            mapped_addr = addr;
            return false;
        }

        public virtual MIRROR mirror()
        {
            return MIRROR.HARDWARE;
        }

        public virtual void reset()
        {

        }

        public virtual bool irqState()
        {
            return false;
        }

        public virtual void irqClear()
        {

        }

        public virtual void scanline()
        {

        }
    }



    // Mapper 000
    public class Mapper_000 : Mapper
    {
        public Mapper_000(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
        {

        }

        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mapped_addr = (ushort)(addr & (PRGbanks > 1 ? 0x7FFF : 0x3FFF));
                data = 0x00;
                return true;
            }
            mapped_addr = addr;
            data = 0x00;
            return false;
        }

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mapped_addr = (ushort)(addr & (PRGbanks > 1 ? 0x7FFF : 0x3FFF));
                return true;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                mapped_addr = addr;
                return true;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            //if (addr >= 0x0000 && addr <= 0x1FFF)
            //{
            //    return true;
            //}
            if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                if (CHRbanks == 0)
                {
                    mapped_addr = addr;
                    return true;
                }
            }
            mapped_addr = addr;
            return false;
        }

        public override void reset()
        {
            base.reset();
        }
    }


    // Alt Mapper 001 -- MMC1
    public class Mapper_001Alt : Mapper
    {
        // Registers
        private byte TempRegister = 0x00;       // Use this to load a value into, when ready, move to "LoadRegister"
        private bool ResetTempRegister = false;
        private int TempRegisterCount = 0;
        private ulong LastCPUStep = 0;

        private byte LoadRegister = 0x00;       // Value that is serial loaded into the mapper
                                                // based on address, could be any of these registers being written

        private byte ControlRegister = 0x00;    // 5 bits - Telling us mirroring, PRG mode and CHR mode
        private byte CHRMode = 0x00;            // 0 = 8k mode (1 bank), 1 = 4k mode (2 banks)
        private byte PRGMode = 0x00;            // 0 = 32k mode (1 bank), 1 = 16k mode (2 banks)
        private byte PRGSelect = 0x00;          // 0 = bank 2 is swappable / 1 = bank 1 is swappable
        private byte MirrorSelect = 0x00;       // select mirror mode - %00 = 1ScA / $01 = 1ScB / %10 = Vert / %11 = horz

        private byte CHRBank0 = 0;              // when in 4k mode, the lower bank, otherwise, the whole bank
        private byte CHRBank1 = 0;              // only second bank for 4k mode
        private ushort PRGBank = 0;             // bank select based on mode

        private bool isSUROM = false;           // if there are 32 banks
        private byte PRG256Block = 0;           // 0 = first 256 k, 1 = second 256k of PRG mem

        private bool RAMEnabled = true;

        private Bus bus;

        public Mapper_001Alt(byte prgBanks, byte chrBanks, Bus inbus) : base(prgBanks, chrBanks)
        {
            // check if SUROM by size
            if (prgBanks >= 32) isSUROM = true;
            else isSUROM = false;

            RAMStatic.AddRange(Enumerable.Repeat<byte>(0x00, 32 * 1024).ToArray());

            bus = inbus;
        }

        public override void reset()
        {
            base.reset();
            TempRegister = 0x00;
            ResetTempRegister = false;
            TempRegisterCount = 0;
            LastCPUStep = 0;

            LoadRegister = 0x00;


            ControlRegister = 0x00;
            CHRMode = 0x00;
            PRGMode = 0x00;
            PRGSelect = 0x00;
            MirrorSelect = 0x00;

            CHRBank0 = 0;
            CHRBank1 = 0;
            PRGBank = 0;

            isSUROM = false;
            PRG256Block = 0;
        }

        public override MIRROR mirror()
        {
            return mirrorMode;
        }

        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            bool returnValue = false;
            mapped_addr = 0x00;
            data = 0x00;

            // $6000 - $7FFF = RAM
            if (addr >= 0x6000 && addr <= 0x7FFF)
            {
                if (RAMEnabled)
                {
                    mapped_addr = 0xFFFF_FFFF;
                    data = RAMStatic[addr & 0x1FFF];
                    returnValue = true;
                }
            }

            // set offset for SUROMs
            int SUoffset = 0;
            if (isSUROM)
            {
                if (PRG256Block == 1)
                {
                    SUoffset = 0x1000_0000;
                }
            }

            // PRGMode 0 
            // $8000 - $FFFF = PRG (check 256 block to select)
            if (addr >= 0x8000 && PRGMode == 0)
            {
                mapped_addr = (uint)(PRGBank * 0x8000 + (addr & 0x7FFF) + SUoffset);
                returnValue = true;
            }

            // PRGMode 1
            // $8000 - $BFFF = PRG 0 (if PRGSelect = 1, this is the swappable area)
            // $C000 - $FFFF = PRG 1 (if PRGSelect = 0, this is the swappable area)
            if (addr >= 0x8000 && PRGMode == 1)
            {
                if (addr >= 0x8000 && addr <= 0xBFFF)
                {
                    if (PRGSelect == 1)
                    {
                        mapped_addr = (uint)(PRGBank * 0x4000 + (addr & 0x3FFF) + SUoffset);
                        returnValue = true;
                    }
                    else
                    {
                        mapped_addr = (uint)(addr + SUoffset);
                    }
                }
                else if (addr >= 0xC000 && addr <= 0xFFFF)
                {
                    if (PRGSelect == 0)
                    {
                        mapped_addr = (uint)(PRGBank * 0x4000 + (addr & 0x3FFF) + SUoffset);
                        returnValue = true;
                    }
                    else
                    {
                        mapped_addr = (uint)(addr + 0x0F00_0000 + SUoffset);
                    }
                }
            }

            return returnValue;
        }

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
        {
            bool returnValue = false;
            mapped_addr = 0x00;

            // Writing to RAM
            if (addr >= 0x6000 && addr <= 0x7FFF)
            {
                if (RAMEnabled)
                {
                    mapped_addr = 0xFFFF_FFFF;
                    RAMStatic[addr & 0x1FFF] = data;
                }
            }

            // writes to 0x8000 + are read in serial fashion
            if (addr >= 0x8000)
            {
                // LoadRegisterValue function returns true if full register is read
                if (LoadRegisterValue(data))
                {
                    if (addr >= 0x8000 && addr <= 0x9FFF)
                    {
                        // Control REgister
                        ControlRegister = (byte)(LoadRegister & 0x1F);

                        switch (ControlRegister & 0x03)
                        {
                            case 0: mirrorMode = MIRROR.ONESCREEN_LO; break;
                            case 1: mirrorMode = MIRROR.ONESCREEN_HI; break;
                            case 2: mirrorMode = MIRROR.VERTICAL; break;
                            case 3: mirrorMode = MIRROR.HORIZONTAL; break;
                        }

                        CHRMode = (byte)(ControlRegister & 0b0001_0000);
                        PRGMode = (byte)(ControlRegister & 0b0000_1000);
                        PRGSelect = (byte)(ControlRegister & 0b0000_0100);

                        returnValue = true;
                    }

                    if (addr >= 0xA000 && addr <= 0xBFFF)
                    {
                        // chr select for one bank
                        if (isSUROM)
                        {
                            if ((LoadRegister & 0b0001_0000) > 0)
                            {
                                PRG256Block = 1;
                            }
                            else
                            {
                                PRG256Block = 0;
                            }

                        }
                       
                        CHRBank0 = (byte)(LoadRegister & 0b0000_1111);
                        
                        returnValue = true;

                    }

                    if (addr >= 0xC000 && addr <= 0xDFFF)
                    {
                        if (!isSUROM && CHRMode == 1)
                        {
                            CHRBank1 = (byte)(LoadRegister & 0b0000_1111);
                            returnValue = true;

                        }
                    }

                    if (addr >= 0xE000 && addr <= 0xFFFF)
                    {
                        RAMEnabled = (LoadRegister & 0xb0001_0000) > 0;

                        PRGBank = (byte)(LoadRegister & 0b0000_1111);
                        returnValue = true;

                    }

                }

            }

            return returnValue;
        }

        private bool LoadRegisterValue(byte data)
        {
            bool returnValue = false; // represents if we have a full value or not

            // check for reset
            if (ResetTempRegister)
            {
                TempRegister = 0x00;
                TempRegisterCount = 0;
                ControlRegister = (byte)(ControlRegister | 0x0C);
                returnValue = false;
            }
            else
            {
                if (bus.cpu.instructCount != LastCPUStep + 1)
                {
                    TempRegister >>= 1;
                    TempRegister |= (byte)((data & 0x01) << 4);
                    TempRegisterCount++;
                    LastCPUStep = bus.cpu.instructCount;
                    if (TempRegisterCount == 5)
                    {
                        LoadRegister = TempRegister;
                        returnValue = true;
                    }
                }

            }

            return returnValue;
        }

        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            bool returnValue = false;
            mapped_addr = 0x00;

            if (addr < 0x2000)
            {
                if (CHRMode == 0)
                {
                    //mapped_addr = (uint)(CHRBank0 * 0x2000 + (addr & 0x1FFF));
                    mapped_addr = addr;
                    returnValue = true;
                }
                else if (CHRMode == 1)
                {
                    if (addr >= 0x0000 && addr <= 0x0FFF)
                    {
                        mapped_addr = (uint)(CHRBank0 * 0x1000 + (addr & 0x0FFF));
                        returnValue = true;
                    }

                    if (addr >= 0x1000 && addr <= 0x1FFF)
                    {
                        mapped_addr = (uint)(CHRBank1 * 0x1000 + (addr & 0x0FFF));
                        returnValue = true;
                    }
                }
            }

            if (mapped_addr > (CHRbanks > 0 ? CHRbanks : 1) * 8192)
            {
                Console.WriteLine("out of sight");
            }

            return returnValue;
        }

        public override bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            bool returnValue = false;
            mapped_addr = 0x00;


            return returnValue;
        }
    }



    // Mapper 001 - MMC1
    public class Mapper_001 : Mapper
    {

        private byte CHRBankSelect4Lo = 0x00;
        private byte CHRBankSelect4Hi = 0x00;
        private byte CHRBankSelect8 = 0x00;

        private byte PRGBankSelect16Lo = 0x00;
        private byte PRGBankSelect16Hi = 0x00;
        private byte PRGBankSelect32 = 0x00;
        private byte PRGBankSelect256 = 0x00;
        private byte PRGBankSelect8 = 0x00;

        private byte LoadRegister = 0x00;
        private byte LoadRegisterCount = 0x00;
        private byte ControlRegister = 0x00;

        private bool LoadJustRead = false;

        private MIRROR mirrorMode = MIRROR.HORIZONTAL;

        //public List<byte> RAMStatic = new List<byte>();

        //public byte[] SaveBytes
        //{
        //    get { return RAMStatic.ToArray(); }
        //    set {
        //        RAMStatic.Clear();
        //        RAMStatic.AddRange(value.ToArray());
        //    }
        //}

        public Mapper_001(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
        {
            //if (File.Exists("savetest.sav"))
            //{
            //    using BinaryReader reader = new BinaryReader(File.OpenRead("savetest.sav"));
            //    {
            //        RAMStatic.AddRange(reader.ReadBytes(32 * 1024).ToArray());
            //    }
            //}
            //else
            //{
            RAMStatic.AddRange(Enumerable.Repeat<byte>(0x00, 32 * 1024).ToArray());
            //}
        }


        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            //if (addr == 8000)
            //{
            //    Console.WriteLine("ds");
            //}
            if (addr >= 0x6000 && addr <= 0x7FFF)
            {
                mapped_addr = 0xFFFFFFFF;

                data = RAMStatic[addr & 0x1FFF];

                return true;
            }

            data = 0x00;
            if (addr >= 0x8000)
            {

                if (PRGbanks == 32)
                {
                    //if ((ControlRegister & 0b10000) > 0)
                    //{
                        mapped_addr = (uint)(PRGBankSelect256 * 0x4000 + (addr & 0x3FFF));
                        return true;
                    //}    
                }
                else
                {
                    if ((ControlRegister & 0b01000) > 0)
                    {
                        // 16 k '
                        if (addr >= 0x8000 && addr <= 0xBFFF)
                        {
                            mapped_addr = (uint)(PRGBankSelect16Lo * 0x4000 + (addr & 0x3FFF));
                            //Log.Debug($"Mapped Read - PRG 16 Lo Bank [{PRGBankSelect16Lo}] - addr [{CPU.Hex(addr,4)}] - mapped [{CPU.Hex((int)mapped_addr,5)}]");
                            return true;
                        }

                        if (addr >= 0xC000 && addr <= 0xFFFF)
                        {
                            mapped_addr = (uint)(PRGBankSelect16Hi * 0x4000 + (addr & 0x3FFF));
                            //Log.Debug($"Mapped Read - PRG 16 Hi Bank [{PRGBankSelect16Hi}] - addr [{CPU.Hex(addr, 4)}] - mapped [{CPU.Hex((int)mapped_addr, 5)}]");
                            return true;
                        }
                    }
                    else
                    {
                        // 32k mode
                        mapped_addr = (uint)(PRGBankSelect32 * 0x8000 + (addr & 0x7FFF));
                        //Log.Debug($"Mapped Read - PRG 32 Bank [{PRGBankSelect32}] - addr [{CPU.Hex(addr, 4)}] - mapped [{CPU.Hex((int)mapped_addr, 5)}]");
                        return true;
                    }
                }
            }
            mapped_addr = addr;
            return false;
        }

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
        {
            if (addr >= 0x6000 && addr <= 0x7FFF)
            {
                mapped_addr = 0xFFFFFFFF;

                RAMStatic[addr & 0x1FFF] = data;



                return true;
            }

            if (addr >= 0x8000)
            {
                if ((data & 0x80) > 0)
                {
                    LoadRegister = 0x00;
                    LoadRegisterCount = 0;
                    ControlRegister = (byte)(ControlRegister | 0x0C);  // 12 - 0b1100
                    //Log.Debug($"Mapper Register Reset");
                }
                else
                {
                    //if (!LoadJustRead)
                    //{
                        // TODO: When the serial port is written to on consecutive cycles, it ignores every write after the first. In practice, this only happens when the CPU executes read-modify-write instructions, which first write the original value before writing the modified one on the next cycle.
                        // TODO: Test what I implemented
                        LoadRegister >>= 1;
                        LoadRegister |= (byte)((data & 0x01) << 4);
                        //LoadRegister |= (byte)((data << 4) & 0x01);
                        LoadRegisterCount++;
                        LoadJustRead = true;
                    //}
                    //else
                    //{
                    //    LoadJustRead = false;
                    //}

                    if (LoadRegisterCount == 5)
                    {
                        //Log.Debug($"Load Register: 0b{Convert.ToString(LoadRegister, toBase: 2).PadLeft(5, '0')}");

                        //byte targetRegister = (byte)((addr >> 13) & 0x03);
                        ushort realTargetReg = addr;

                        if (PRGbanks == 32)
                        {
                            if (realTargetReg >= 0x8000 && realTargetReg <= 0x9FFF)     // Configuration Register Set
                            {
                                ControlRegister = (byte)(LoadRegister & 0x1F);

                                switch (ControlRegister & 0x03)
                                {
                                    case 0: mirrorMode = MIRROR.ONESCREEN_LO; break;
                                    case 1: mirrorMode = MIRROR.ONESCREEN_HI; break;
                                    case 2: mirrorMode = MIRROR.VERTICAL; break;
                                    case 3: mirrorMode = MIRROR.HORIZONTAL; break;
                                }
                                //MMC1PRGBankUpdate();

                                //Log.Debug($"Mirror Mode set: {((byte)mirrorMode)}");
                            }
                            else if (realTargetReg >= 0xA000 && realTargetReg <= 0xBFFF)     // CHR Bank 0
                            {
                                // if this is an SOROM, SUROM or SXROM, select prg too
                                ControlRegister = (byte)(ControlRegister & 31);

                                //MMC1PRGBankUpdate();



                                //if ((ControlRegister & 0b10000) > 0)
                                //{
                                    // select 256 bank
                                    PRGBankSelect256 = (byte)(LoadRegister & 0x1F);
                                    //CHRBankSelect4Lo = (byte)(LoadRegister & 0x1F);
                                //}
                                //else if ((ControlRegister & 0b01100) > 0)
                                //{
                                //    PRGBankSelect8 = (byte)(LoadRegister & 0x1F);
                                //}

                                //if ((ControlRegister & 0b00001) > 0)
                                //{
                                //    CHRBankSelect4Lo = (byte)((LoadRegister & 0x1E) >> 1);
                                //    //CHRBankSelect8 = (byte)(LoadRegister & 15);

                                //}
                            }
                            //else if (realTargetReg >= 0xC000 && realTargetReg <= 0xDFFF)     // CHR Bank 1
                            //{
                            //    if ((ControlRegister & 0b00001) > 0)
                            //    {
                            //        CHRBankSelect4Hi = (byte)(LoadRegister & 0x1F);
                            //    }
                            //}
                            else if (realTargetReg >= 0xE000 && realTargetReg <= 0xFFFF)     // PRG Bank
                            {
                                LoadRegister = (byte)(LoadRegister & 15);
                                //MMC1PRGBankUpdate();


                            }
                        }
                        else
                        {

                            if (realTargetReg >= 0x8000 && realTargetReg <= 0x9FFF)     // Configuration Register Set
                            {
                                ControlRegister = (byte)(LoadRegister & 0x1F);

                                switch (ControlRegister & 0x03)
                                {
                                    case 0: mirrorMode = MIRROR.ONESCREEN_LO; break;
                                    case 1: mirrorMode = MIRROR.ONESCREEN_HI; break;
                                    case 2: mirrorMode = MIRROR.VERTICAL; break;
                                    case 3: mirrorMode = MIRROR.HORIZONTAL; break;
                                }
                                //MMC1PRGBankUpdate();

                                //Log.Debug($"Mirror Mode set: {((byte)mirrorMode)}");
                            }
                            else if (realTargetReg >= 0xA000 && realTargetReg <= 0xBFFF)     // CHR Bank 0
                            {
                                // if this is an SOROM, SUROM or SXROM, select prg too
                                ControlRegister = (byte)(ControlRegister & 31);

                                //MMC1PRGBankUpdate();

                                if ((ControlRegister & 0b10000) > 0)
                                {
                                    CHRBankSelect4Lo = (byte)(LoadRegister & 0x1F);
                                }
                                else
                                {
                                    CHRBankSelect8 = (byte)((LoadRegister & 0x1E) >> 1);
                                    //CHRBankSelect8 = (byte)(LoadRegister & 15);

                                }
                            }
                            else if (realTargetReg >= 0xC000 && realTargetReg <= 0xDFFF)     // CHR Bank 1
                            {
                                if ((ControlRegister & 0b10000) > 0)
                                {
                                    CHRBankSelect4Hi = (byte)(LoadRegister & 0x1F);
                                }
                            }
                            else if (realTargetReg >= 0xE000 && realTargetReg <= 0xFFFF)     // PRG Bank
                            {
                                LoadRegister = (byte)(LoadRegister & 15);
                                MMC1PRGBankUpdate();


                            }
                        }

                        //if (targetRegister == 0)
                        //{
                        //    ControlRegister = (byte)(LoadRegister & 0x1F);

                        //    switch (ControlRegister & 0x03)
                        //    {
                        //        case 0: mirrorMode = MIRROR.ONESCREEN_LO; break;
                        //        case 1: mirrorMode = MIRROR.ONESCREEN_HI; break;
                        //        case 2: mirrorMode = MIRROR.VERTICAL; break;
                        //        case 3: mirrorMode = MIRROR.HORIZONTAL; break;
                        //    }
                        //    Log.Debug($"Mirror Mode set: {((byte)mirrorMode)}");
                        //}
                        //else if (targetRegister == 1)
                        //{
                        //    if ((ControlRegister & 0b10000) > 0)
                        //    {
                        //        CHRBankSelect4Lo = (byte)(LoadRegister & 0x1F);
                        //        Log.Debug($"CHR Bank Select 4 lo = {CHRBankSelect4Lo}");
                        //    }
                        //    else
                        //    {
                        //        CHRBankSelect8 = (byte)(LoadRegister & 0x1E);
                        //        Log.Debug($"CHR Bank Select 8 = {CHRBankSelect8}");

                        //    }
                        //}
                        //else if (targetRegister == 2)
                        //{
                        //    if ((ControlRegister & 0b10000) > 0)
                        //    {
                        //        CHRBankSelect4Hi = (byte)(LoadRegister & 0x1F);
                        //        Log.Debug($"CHR Bank Select 4 Hi = {CHRBankSelect4Hi}");
                        //    }
                        //}
                        //else if (targetRegister == 3)
                        //{
                        //    byte PRGMode = (byte)((ControlRegister >> 2) & 0x03);

                        //    if (PRGMode == 0 || PRGMode == 1)
                        //    {
                        //        PRGBankSelect32 = (byte)((LoadRegister & 0x0E) >> 1);
                        //        Log.Debug($"PRG Bank Select 32 = {PRGBankSelect32}");
                        //    }
                        //    else if (PRGMode == 2)
                        //    {
                        //        PRGBankSelect16Lo = 0;
                        //        PRGBankSelect16Hi = (byte)(LoadRegister & 0x0F);
                        //        Log.Debug($"PRG Bank Select 16 Lo = 0 and 16 hi = {PRGBankSelect16Hi}");
                        //    }
                        //    else if (PRGMode == 3)
                        //    {
                        //        // -----
                        //        PRGBankSelect16Lo = (byte)(LoadRegister & 0x0F);
                        //        // -----

                        //        PRGBankSelect16Hi = (byte)(PRGbanks - 1);
                        //        Log.Debug($"PRG Bank Select 16 Lo = {PRGBankSelect16Lo} and 16 hi = {PRGBankSelect16Hi}");

                        //    }
                        //}

                        LoadRegister = 0x00;
                        LoadRegisterCount = 0;
                    }
                }
            }

            mapped_addr = addr;
            data = 0x00;
            return false;
        }

        public void MMC1PRGBankUpdate()
        {
            byte PRGMode = (byte)((ControlRegister >> 2) & 0x03);

            if (PRGbanks == 32)
            {

                if ((ControlRegister & 0b10000) > 0)
                {
                    // select 256 bank
                    PRGBankSelect256 = (byte)(LoadRegister & 0x1F);
                    //CHRBankSelect4Lo = (byte)(LoadRegister & 0x1F);
                }
                else if ((ControlRegister & 0b01100) > 0)
                {
                    PRGBankSelect8 = (byte)(LoadRegister & 0x1F);
                }
            }
            else
            {
                if (PRGMode == 0 || PRGMode == 1)
                {
                    PRGBankSelect32 = (byte)((LoadRegister & 0x0E) >> 1);
                    //Log.Debug($"PRG Bank Select 32 = {PRGBankSelect32}");
                }
                else if (PRGMode == 2)
                {
                    PRGBankSelect16Lo = 0;
                    PRGBankSelect16Hi = (byte)(LoadRegister & 0x0F);
                    //Log.Debug($"PRG Bank Select 16 Lo = 0 and 16 hi = {PRGBankSelect16Hi}");
                }
                else if (PRGMode == 3)
                {
                    // -----
                    PRGBankSelect16Lo = (byte)(LoadRegister & 0x0F);
                    // -----

                    PRGBankSelect16Hi = (byte)(PRGbanks - 1);
                    //Log.Debug($"PRG Bank Select 16 Lo = {PRGBankSelect16Lo} and 16 hi = {PRGBankSelect16Hi}");

                }
            }
        }

        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr == 8000 && CHRBankSelect8 == 16)
            {
                Console.WriteLine("ds");
            }
            if (addr < 0x2000)
            {
                if (CHRbanks == 0)
                {
                    mapped_addr = addr;
                    return true;
                }
                else
                {

                    if ((ControlRegister & 0b10000) > 0)
                    {
                        // 4k chr bank mode
                        if (addr >= 0x0000 && addr <= 0x0FFF)
                        {
                            mapped_addr = (uint)(CHRBankSelect4Lo * 0x1000 + (addr & 0x0FFF));
                            //Log.Debug($"Mapped Read CHR 4 Lo: [{CHRBankSelect4Lo}] - addr [{CPU.Hex(addr, 4)}] - mapped [{CPU.Hex((int)mapped_addr, 5)}]");
                            return true;
                        }

                        if (addr >= 0x1000 && addr <= 0x1FFF)
                        {
                            mapped_addr = (uint)(CHRBankSelect4Hi * 0x1000 + (addr & 0x0FFF));
                            //Log.Debug($"Mapped Read CHR 4 Hi: [{CHRBankSelect4Hi}] - addr [{CPU.Hex(addr, 4)}] - mapped [{CPU.Hex((int)mapped_addr, 5)}]");

                            return true;
                        }
                    }
                    else
                    {
                        // 8k
                        mapped_addr = (uint)((CHRBankSelect8) * 0x2000 + (addr & 0x1FFF));
                        //Log.Debug($"Mapped Read CHR 8: [{CHRBankSelect8}] - addr [{CPU.Hex(addr, 4)}] - mapped [{CPU.Hex((int)mapped_addr, 5)}]");

                        return true;
                    }
                }
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            if (addr < 0x2000)
            {
                if (CHRbanks == 0)
                {
                    mapped_addr = addr;
                    return true;
                }
                mapped_addr = addr;
                return true;

            }
            else
            {
                mapped_addr = addr;
                return false;
            }
        }

        public override void reset()
        {
            ControlRegister = 0x1C;
            LoadRegister = 0x00;
            LoadRegisterCount = 0x00;

            CHRBankSelect4Lo = 0x00;
            CHRBankSelect4Hi = 0x00;
            CHRBankSelect8 = 0x00;

            PRGBankSelect16Lo = 0x00;
            PRGBankSelect16Hi = (byte)(PRGbanks - 1);
            PRGBankSelect32 = 0x00;

            base.reset();
        }

        public override MIRROR mirror()
        {
            return mirrorMode;
        }

    }


    // Mapper 002
    public class Mapper_002 : Mapper
    {
        private byte PRGBankSelectLo = 0x00;
        private byte PRGBankSelectHi = 0x00;

        public Mapper_002(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
        {
        }

        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            if (addr >= 0x8000 && addr <= 0xBFFF)
            {
                mapped_addr = (uint)(PRGBankSelectLo * 0x4000 + (addr & 0x3FFF));
                data = 0x00;
                return true;
            }

            if (addr >= 0xC000 && addr <= 0xFFFF)
            {
                //Console.WriteLine($"mapped = {CPU.Hex((int)addr, 8)}");

                mapped_addr = (uint)(PRGBankSelectHi * 0x4000 + (addr & 0x3FFF));
                //Console.WriteLine($"mapped = {CPU.Hex((int)mapped_addr, 8)}");
                data = 0x00;
                return true;
            }

            mapped_addr = addr;
            data = 0x00;
            return false;
        }

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                PRGBankSelectLo = (byte)(data & 0x0F);
                //mapped_addr = (ushort)(addr & (PRGbanks > 1 ? 0x7FFF : 0x3FFF));
                //return true;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr < 0x2000)
            {
                mapped_addr = addr;
                return true;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            if (addr < 0x2000)
            {
                if (CHRbanks == 0)
                {
                    mapped_addr = addr;
                    return true;
                }
            }
            mapped_addr = addr;
            return false;
        }

        public override void reset()
        {
            PRGBankSelectLo = 0;
            PRGBankSelectHi = (byte)(PRGbanks - 1);
            base.reset();
        }

    }


    // Mapper 003
    public class Mapper_003 : Mapper
    {
        private byte CHRBankSelect = 0x00;

        public Mapper_003(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
        {
        }

        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mapped_addr = (ushort)(addr & (PRGbanks > 1 ? 0x7FFF : 0x3FFF));
                data = 0x00;
                return true;
            }
            mapped_addr = addr;
            data = 0x00;
            return false;
        }

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                CHRBankSelect = (byte)(data & 0x03);
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr < 0x2000)
            {
                mapped_addr = (uint)(CHRBankSelect * 0x2000 + addr);
                return true;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            mapped_addr = addr;
            return false;
        }


        public override void reset()
        {
            CHRBankSelect = 0;
            base.reset();
        }

    }


    // Mapper 004
    public class Mapper_004 : Mapper
    {
        private byte targetRegister = 0x00;
        private bool PRGBankMode = false;
        private bool CHRInversion = false;
        private MIRROR mirrorMode = MIRROR.HORIZONTAL;

        private uint[] Register = new uint[8];
        private uint[] CHRBank = new uint[8];
        private uint[] PRGBank = new uint[4];

        private bool IRQActive = false;
        private bool IRQEnable = false;
        private bool IRQUpdate = false;
        private uint IRQCounter = 0x0000;
        private uint IRQReload = 0x0000;

        //private List<byte> RAMStatic = new List<byte>();


        public Mapper_004(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
        {
            RAMStatic.AddRange(Enumerable.Repeat<byte>(0x00, 8 * 1024).ToArray());
        }

        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            data = 0x00;

            if (addr >= 0x6000 && addr <= 0x7FFF)
            {
                mapped_addr = 0xFFFFFFFF;
                data = RAMStatic[addr & 0x1FFF];
                return true;
            }

            if (addr >= 0x8000 && addr <= 0x9FFF)
            {
                mapped_addr = (uint)(PRGBank[0] + (addr & 0x1FFF));
                return true;
            }

            if (addr >= 0xA000 && addr <= 0xBFFF)
            {
                mapped_addr = (uint)(PRGBank[1] + (addr & 0x1FFF));
                return true;
            }

            if (addr >= 0xC000 && addr <= 0xDFFF)
            {
                mapped_addr = (uint)(PRGBank[2] + (addr & 0x1FFF));
                return true;
            }

            if (addr >= 0xE000 && addr <= 0xFFFF)
            {
                mapped_addr = (uint)(PRGBank[3] + (addr & 0x1FFF));
                return true;
            }

            mapped_addr = addr;
            return false;
        }

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
        {
            if (addr >= 0x6000 && addr <= 0x7FFF)
            {
                mapped_addr = 0xFFFFFFFF;
                RAMStatic[addr & 0x1FFF] = data;
                return true;
            }

            if (addr >= 0x8000 && addr <= 0x9FFF)
            {
                if ((addr & 0x0001) == 0)
                {
                    targetRegister = (byte)(data & 0x07);
                    PRGBankMode = (data & 0x40) > 0;
                    CHRInversion = (data & 0x80) > 0;
                }
                else
                {
                    Register[targetRegister] = data;

                    if (CHRInversion)
                    {
                        CHRBank[0] = Register[2] * 0x0400;
                        CHRBank[1] = Register[3] * 0x0400;
                        CHRBank[2] = Register[4] * 0x0400;
                        CHRBank[3] = Register[5] * 0x0400;
                        CHRBank[4] = (Register[0] & 0xFE) * 0x0400;
                        CHRBank[5] = Register[0] * 0x0400 + 0x0400;
                        CHRBank[6] = (Register[1] & 0xFE) * 0x0400;
                        CHRBank[7] = Register[1] * 0x0400 + 0x0400;
                    }
                    else
                    {
                        CHRBank[0] = (Register[0] & 0xFE) * 0x0400;
                        CHRBank[1] = Register[0] * 0x0400 + 0x0400;
                        CHRBank[2] = (Register[1] & 0xFE) * 0x0400;
                        CHRBank[3] = Register[1] * 0x0400 + 0x0400;

                        //CHRBank[0] = 
                        //CHRBank[1] = 
                        //CHRBank[2] = 
                        //CHRBank[3] = 
                        CHRBank[4] = Register[2] * 0x0400;
                        CHRBank[5] = Register[3] * 0x0400;
                        CHRBank[6] = Register[4] * 0x0400;
                        CHRBank[7] = Register[5] * 0x0400;
                    }

                    if (PRGBankMode)
                    {
                        PRGBank[2] = (Register[6] & 0x3F) * 0x2000;
                        PRGBank[0] = (uint)((PRGbanks * 2 - 2) * 0x2000);
                    }
                    else
                    {
                        PRGBank[0] = (Register[6] & 0x3F) * 0x2000;
                        PRGBank[2] = (uint)((PRGbanks * 2 - 2) * 0x2000);
                    }

                    PRGBank[1] = (Register[7] & 0x3F) * 0x2000;
                    PRGBank[3] = (uint)((PRGbanks * 2 - 1) * 0x2000);
                }

                mapped_addr = addr;
                return false;
            }

            if (addr >= 0xA000 && addr <= 0xBFFF)
            {
                if ((addr & 0x0001) == 0)
                {
                    if ((data & 0x01) > 0)
                    {
                        mirrorMode = MIRROR.HORIZONTAL;
                    }
                    else
                    {
                        mirrorMode = MIRROR.VERTICAL;
                    }
                }
                else
                {
                    //PRG Ram protect TODO

                }
                mapped_addr = addr;
                return false;
            }

            if (addr >= 0xC000 && addr <= 0xDFFF)
            {
                if ((addr & 0x0001) == 0)
                {
                    IRQReload = data;
                }
                else
                {
                    IRQCounter = 0x0000;
                }
                mapped_addr = addr;
                return false;
            }

            if (addr >= 0xE000 && addr <= 0xFFFF)
            {
                if ((addr & 0x0001) == 0)
                {
                    IRQEnable = false;
                    IRQActive = false;
                }
                else
                {
                    IRQEnable = true;
                }
                mapped_addr = addr;
                return false;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr >= 0x0000 && addr <= 0x03FF)
            {
                mapped_addr = (uint)(CHRBank[0] + (addr & 0x03FF));
                return true;
            }

            if (addr >= 0x0400 && addr <= 0x07FF)
            {
                mapped_addr = (uint)(CHRBank[1] + (addr & 0x03FF));
                return true;

            }

            if (addr >= 0x0800 && addr <= 0x0BFF)
            {
                mapped_addr = (uint)(CHRBank[2] + (addr & 0x03FF));
                return true;
            }

            if (addr >= 0x0C00 && addr <= 0x0FFF)
            {
                mapped_addr = (uint)(CHRBank[3] + (addr & 0x03FF));
                return true;
            }

            if (addr >= 0x1000 && addr <= 0x13FF)
            {
                mapped_addr = (uint)(CHRBank[4] + (addr & 0x03FF));
                return true;
            }

            if (addr >= 0x1400 && addr <= 0x17FF)
            {
                mapped_addr = (uint)(CHRBank[5] + (addr & 0x03FF));
                return true;
            }

            if (addr >= 0x1800 && addr <= 0x1BFF)
            {
                mapped_addr = (uint)(CHRBank[6] + (addr & 0x03FF));
                return true;
            }

            if (addr >= 0x1C00 && addr <= 0x1FFF)
            {
                mapped_addr = (uint)(CHRBank[7] + (addr & 0x03FF));
                return true;
            }

            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            mapped_addr = addr;
            return false;
        }

        public override void reset()
        {
            targetRegister = 0x00;
            PRGBankMode = false;
            CHRInversion = false;
            mirrorMode = MIRROR.HORIZONTAL;

            IRQActive = false;
            IRQEnable = false;
            IRQUpdate = false;
            IRQCounter = 0x0000;
            IRQReload = 0x0000;

            for (int i = 0; i < 4; i++) PRGBank[i] = 0;
            for (int i = 0; i < 8; i++) { CHRBank[i] = 0; Register[i] = 0; }

            PRGBank[0] = 0 * 0x2000;
            PRGBank[1] = 1 * 0x2000;
            PRGBank[2] = (uint)((PRGbanks * 2 - 2) * 0x2000);
            PRGBank[3] = (uint)((PRGbanks * 2 - 1) * 0x2000);

            base.reset();
        }

        public override MIRROR mirror()
        {
            return base.mirror();
        }

        public override bool irqState()
        {
            return IRQActive;
        }

        public override void irqClear()
        {
            IRQActive = false;
        }

        public override void scanline()
        {
            if (IRQCounter == 0)
            {
                IRQCounter = IRQReload;
            }
            else
                IRQCounter--;

            if (IRQCounter == 0 && IRQEnable)
            {
                IRQActive = true;
            }
        }
    }


    // Mapper 009 - MMC2
    public class Mapper_009 : Mapper
    {

        private uint[] CHRBank0 = new uint[2];
        private uint[] CHRBank1 = new uint[2];
        private uint[] PRGBank = new uint[4];

        //private byte latch0 = 0xFD;
        //private byte latch1 = 0xFD;
        private bool latch0 = false;
        private bool latch1 = false;

        public Mapper_009(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
        {
            RAMStatic.AddRange(Enumerable.Repeat<byte>(0x00, 8 * 1024).ToArray());
            mirrorMode = MIRROR.HORIZONTAL;
        }

        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            data = 0x00;

            if (addr >= 0x6000 && addr <= 0x7FFF)
            {
                mapped_addr = 0xFFFFFFFF;
                data = RAMStatic[addr & 0x1FFF];
                return true;
            }

            if (addr >= 0x8000 && addr <= 0x9FFF)
            {
                mapped_addr = (uint)((addr - 0x8000) + PRGBank[0] * 0x2000);
                return true;
            }
            else if (addr >= 0xA000 && addr <= 0xBFFF)
            {
                mapped_addr = (uint)((addr - 0xA000) + PRGBank[1] * 0x2000);
                return true;
            }
            else if (addr >= 0xC000 && addr <= 0xDFFF)
            {
                mapped_addr = (uint)((addr - 0xC000) + PRGBank[2] * 0x2000);
                return true;
            }
            else if (addr >= 0xE000 && addr <= 0xFFFF)
            {
                mapped_addr = (uint)((addr - 0xE000) + PRGBank[3] * 0x2000);
                return true;
            }


            mapped_addr = addr;
            return false;
        }

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
        {
            if (addr >= 0x6000 && addr <= 0x7FFF)
            {
                mapped_addr = 0xFFFFFFFF;
                //RAMStatic[addr & 0x1FFF] = data;
                RAMStatic[addr - 0x6000] = data;
                return true;
            }

            if (addr >= 0xA000 && addr <= 0xAFFF)
            {
                // PRG ROM bank select
                var selected = (addr & 0x0F);
                //if (selected != 15)
                //{
                //    Console.WriteLine("selcted");
                //}
                Console.WriteLine($"PRG Select: {selected}");
                PRGBank[0] = (byte)(selected);
            }
            else if (addr >= 0xB000 && addr <= 0xBFFF)
            {
                CHRBank0[0] = (byte)((addr & 0x1F));
            }
            else if (addr >= 0xC000 && addr <= 0xCFFF)
            {
                CHRBank0[1] = (byte)((addr & 0x1F));
            }
            else if (addr >= 0xD000 && addr <= 0xDFFF)
            {
                CHRBank1[0] = (byte)((addr & 0x1F));

            }
            else if (addr >= 0xE000 && addr <= 0xEFFF)
            {
                CHRBank1[1] = (byte)((addr & 0x1F));
            }

            if (addr >= 0xF000 && addr <= 0xFFFF)
            {
                // mirroring
                /// 7  bit  0
                /// -------
                /// xxxx xxxM
                ///         |
                ///         +-Select nametable mirroring(0: vertical; 1: horizontal)
                ///

                if ((data & 0x01) > 0)
                {
                    mirrorMode = MIRROR.HORIZONTAL;
                }
                else
                {
                    mirrorMode = MIRROR.VERTICAL;
                }
            }

            mapped_addr = addr;
            return false;
        }



        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {

            //if (addr == 4096 && (CHRBank1[0] > 0 || CHRBank1[1] > 0))
            //{
            //    Console.WriteLine("4096");
            //}


            if (addr >= 0x0 && addr <= 0x0FFF)
            {
                mapped_addr = addr;
                if (latch0)
                {
                    mapped_addr = (uint)(addr + 0xC000);
                }
                else
                {
                    mapped_addr = (uint)(addr + 0xB000);

                }



                //if (latch0 == 0xFD)
                //{
                //    mapped_addr = addr + CHRBank0[0] * 0x1000;
                //}
                //else if (latch0 == 0xFE)
                //{
                //    mapped_addr = addr + CHRBank0[1] * 0x1000;
                //}

                if (addr >= 0x0FD0 && addr <= 0x0FDF)
                {
                    // latch 0 set to $FD
                    //latch0 = 0xFD;
                    latch0 = false;
                }

                //if (addr == 0x0FE8)
                if (addr >= 0x0FE0 && addr <= 0x0FEF)
                {
                    // latch 0 set to $FE
                    //latch0 = 0xFE;
                    latch0 = true;

                }

                return true;
            }
            else if (addr >= 0x1000 && addr <= 0x1FFF)
            {
                mapped_addr = addr;
                if (latch1)
                {
                    mapped_addr = (uint)(addr + 0xE000);
                }
                else
                {
                    mapped_addr = (uint)(addr + 0xD000);

                }


                //if (latch1 == 0xFD)
                //{
                //    mapped_addr = addr + CHRBank1[0] * 0x1000;
                //}
                //else if (latch1 == 0xFE)
                //{
                //    mapped_addr = addr + CHRBank1[1] * 0x1000;
                //}

                if (addr >= 0x1FD0 && addr <= 0x1FDF)
                {
                    // latch 1 set to $FD
                    //latch1 = 0xFD;
                    latch1 = false;
                }

                else if (addr >= 0x1FE0 && addr <= 0x1FEF)
                {
                    // latch 1 set to $FE
                    //latch1 = 0xFE;
                    latch1 = true;
                }

                return true;
            }

            mapped_addr = addr;
            return false;

        }

        public override bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            return base.ppuMapWrite(addr, out mapped_addr);
        }

        public override void reset()
        {
            mirrorMode = MIRROR.HORIZONTAL;

            for (int i = 0; i < 4; i++) { PRGBank[i] = 0; }
            for (int i = 0; i < 2; i++) { CHRBank0[i] = 0; }
            for (int i = 0; i < 2; i++) { CHRBank1[i] = 0; }

            PRGBank[0] = 0x00;
            PRGBank[1] = (uint)((PRGbanks - 3));
            PRGBank[2] = (uint)((PRGbanks - 2));
            PRGBank[3] = (uint)((PRGbanks - 1));

            //latch1 = 0xFD;
            //latch0 = 0xFD;
            latch1 = false;
            latch0 = false;

            base.reset();
        }

        public override MIRROR mirror()
        {
            return mirrorMode;
        }

    }




}

