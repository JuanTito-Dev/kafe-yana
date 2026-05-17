namespace KafeYana.Infrastructure.Options
{
    public class CloudflareR2Options
    {
        public const string Key = "CloudflareR2";

        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;

        /// <summary>
        /// URL pública del bucket (ej: https://pub-xxx.r2.dev o dominio personalizado).
        /// No debe terminar en "/"
        /// </summary>
        public string PublicUrl { get; set; } = string.Empty;
    }
}
