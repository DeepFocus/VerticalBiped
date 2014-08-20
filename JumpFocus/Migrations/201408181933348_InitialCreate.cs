namespace JumpFocus.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Histories",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Played = c.DateTime(nullable: false),
                        Altitude = c.Int(nullable: false),
                        Dogecoins = c.Int(nullable: false),
                        Picture = c.String(),
                        Ped = c.Int(nullable: false),
                        JetPack = c.Int(nullable: false),
                        Helmet = c.Int(nullable: false),
                        Player_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Players", t => t.Player_Id)
                .Index(t => t.Player_Id);
            
            CreateTable(
                "dbo.Players",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        TwitterId = c.Long(nullable: false),
                        TwitterHandle = c.String(),
                        TwitterPhoto = c.String(),
                        Dogecoins = c.Int(nullable: false),
                        Mugshot = c.String(),
                        Created = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Histories", "Player_Id", "dbo.Players");
            DropIndex("dbo.Histories", new[] { "Player_Id" });
            DropTable("dbo.Players");
            DropTable("dbo.Histories");
        }
    }
}
