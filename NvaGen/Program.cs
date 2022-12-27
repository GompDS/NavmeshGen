using System.Numerics;
using SoulsFormats;

namespace NvaGen;

public class Program
{
    public static void Main(string[] args)
    {
        string? nvaPath = args.FirstOrDefault(NVA.Is);
        NVA nva = new(DCX.Type.DCX_DFLT_10000_44_9);
        NVA nvaBackup = new(DCX.Type.DCX_DFLT_10000_44_9);
        if (File.Exists(nvaPath))
        {
            if (NVA.Is(nvaPath))
            {
                nva = NVA.Read(nvaPath);
                nvaBackup = NVA.Read(nvaPath);
            }
        }

        string? nvmhktbndPath = args.FirstOrDefault(x => BND4.Is(x) && x.EndsWith("nvmhktbnd.dcx"));
        if (nvmhktbndPath == null)
        {
            throw new ArgumentException("No nvmhktbnd included in program arguments.");
        }
        BND4 nvmhktbnd = BND4.Read(nvmhktbndPath);
        
        string? msbPath = args.FirstOrDefault(MSB3.Is);
        if (msbPath == null)
        {
            throw new ArgumentException("No msb included in program arguments.");
        }
        MSB3 msb = MSB3.Read(msbPath);

        foreach (BinderFile file in nvmhktbnd.Files.Where(x => 
                     nva.Navmeshes.All(y => y.ModelID != int.Parse(Path.GetFileName(x.Name)[13..19]))))
        {
            int area = int.Parse(Path.GetFileName(file.Name)[1..3]);
            int block = int.Parse(Path.GetFileName(file.Name)[4..6]);
            int model = int.Parse(Path.GetFileName(file.Name)[13..19]);
            AddNavMeshToNva(area, block, model, nva, msb, nvmhktbnd);
        }
        
        string nvaName = nvmhktbndPath.Replace("nvmhktbnd", "nva");
        if (!File.Exists($"{nvaName}.bak") && nvaPath != null)
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
                Position = new Vector3(),//col.Position,
                Rotation = new Vector3(),//col.Rotation,
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