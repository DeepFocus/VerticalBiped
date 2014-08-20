namespace JumpFocus.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MoveMugshot : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Histories", "Mugshot", c => c.String());
            DropColumn("dbo.Histories", "Picture");
            DropColumn("dbo.Histories", "Ped");
            DropColumn("dbo.Histories", "JetPack");
            DropColumn("dbo.Histories", "Helmet");
            DropColumn("dbo.Players", "Mugshot");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Players", "Mugshot", c => c.String());
            AddColumn("dbo.Histories", "Helmet", c => c.Int(nullable: false));
            AddColumn("dbo.Histories", "JetPack", c => c.Int(nullable: false));
            AddColumn("dbo.Histories", "Ped", c => c.Int(nullable: false));
            AddColumn("dbo.Histories", "Picture", c => c.String());
            DropColumn("dbo.Histories", "Mugshot");
        }
    }
}
