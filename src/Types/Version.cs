namespace DownloadBDS.Types;

public struct Version(int major, int minor, int build, int revision)
{
    public int Major { get; set; } = major;
    public int Minor { get; set; } = minor;
    public int Build { get; set; } = build;
    public int Revision { get; set; } = revision;

    public override readonly string ToString()
    {
        return
            $"{Major}.{Minor}.{Build}.{(((Minor > 15 && Build > 0) || Minor > 16) && Revision is > 0 and < 10 ? "0" : string.Empty)}{Revision}";
    }

    public static bool TryParse(string? input, out Version version)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            version = default;
            return false;
        }

        string[] nums = input.Split('.');
        version = new(default, default, default, default);
        for (int i = 0; i < nums.Length; i++)
        {
            int num;
            try
            {
                num = Convert.ToInt32(nums[i]);
            }
            catch
            {
                continue;
            }

            switch (i)
            {
                case 0:
                    {
                        version.Major = num;
                        break;
                    }
                case 1:
                    {
                        version.Minor = num;
                        break;
                    }
                case 2:
                    {
                        version.Build = num;
                        break;
                    }
                case 3:
                    {
                        version.Revision = num;
                        break;
                    }
                default:
                    {
                        goto END;
                    }
            }
        }

        END:
        return true;
    }
}