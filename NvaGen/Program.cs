using SoulsFormats;

namespace NvaGen;

public class Program
{
    public static void Main(string[] args)
    {
        if (!NVA.IsRead(args.FirstOrDefault(x => NVA.Is(x), ""), out NVA nva))
        {
            nva = new NVA();
        }

        NVA nvaBackup = NVA.Read(args.FirstOrDefault(x => NVA.Is(x)));
        
        if (!BND4.IsRead(args.FirstOrDefault(x => x.EndsWith("nvmhktbnd.dcx"), ""), out BND4 nvmhktbnd))
        {
            throw new ArgumentException("No nvmhktbnd included in program arguments.");
        }
        
        if (!MSB3.IsRead(args.FirstOrDefault(x => MSB3.Is(x), ""), out MSB3 msb))
        {
            throw new ArgumentException("No msb included in program arguments.");
        }
        
        foreach (BinderFile file in nvmhktbnd.Files.Where(x => 
                     nva.Navmeshes.All(y => y.ModelID != int.Parse(Path.GetFileName(x.Name)[13..19]))))
        {
            int area = int.Parse(Path.GetFileName(file.Name)[1..3]);
            int block = int.Parse(Path.GetFileName(file.Name)[4..6]);
            int model = int.Parse(Path.GetFileName(file.Name)[13..19]);
            AddNavMeshToNva(area, block, model, nva, msb, nvmhktbnd);
        }
        
        string nvaName = args.FirstOrDefault(x => NVA.Is(x), "");
        if (!File.Exists($"{nvaName}.bak"))
        {
            nvaBackup.Write($"{nvaName}.bak");
        }
        nva.Write($"{nvaName}");
    }

    private static void AddNavMeshToNva(int area, int block, int model, NVA nva, MSB3 msb, BND4 nvmhktbnd)
    {
        BinderFile? navBinderFile = nvmhktbnd.Files.FirstOrDefault(x => x.Name.EndsWith(model.ToString("D6") + ".hkx"));
        if (navBinderFile == null)
        {
            return;
        }

        BinaryReaderEx br = new (false, navBinderFile.Bytes);
        for (int i = 0; i < 0x308; i += 4)
        {
            br.ReadUInt32();
        }
        int triangleCount = (int) br.ReadUInt32();
        if (triangleCount == 1936671604)
        {
            for (int i = 0x30C; i < 0x368; i += 4)
            {
                br.ReadUInt32();
            }
            triangleCount = (int) br.ReadUInt32();
        }
        
        foreach (MSB3.Part.Collision col in msb.Parts.Collisions.Where(x => int.Parse(x.ModelName[1..]) == model))
        {
            NVA.Navmesh navmesh = new()
            {
                MapNodeCount = -1,
                NameID = int.Parse($"{area}{block}{model:D6}"),
                ModelID = model,
                Position = col.Position,
                Rotation = col.Rotation,
                TriangleCount = triangleCount,
                Unk4C = false
            };
            for (int j = 0; j < navmesh.TriangleCount; j++) {
                nva.Entries1.Add(new NVA.Entry1());
            }
            nva.Navmeshes.Add(navmesh);
        }
    }
}