using Microsoft.EntityFrameworkCore;
using Workflow.ApiService.Data.Entities;

namespace Workflow.ApiService.Data;

public class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowDefinitionEntity> WorkflowDefinitions => Set<WorkflowDefinitionEntity>();
    public DbSet<WorkflowInstanceEntity> WorkflowInstances => Set<WorkflowInstanceEntity>();
    public DbSet<ActivityExecutionLogEntity> ActivityExecutionLogs => Set<ActivityExecutionLogEntity>();
    public DbSet<UserTaskEntity> UserTasks => Set<UserTaskEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowDefinitionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.DefinitionJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<WorkflowInstanceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StateJson).HasColumnType("jsonb");
            entity.HasOne(e => e.WorkflowDefinition)
                .WithMany(d => d.Instances)
                .HasForeignKey(e => e.WorkflowDefinitionId);
        });

        modelBuilder.Entity<ActivityExecutionLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.InputJson).HasColumnType("jsonb");
            entity.Property(e => e.OutputJson).HasColumnType("jsonb");
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany(i => i.ActivityExecutionLogs)
                .HasForeignKey(e => e.WorkflowInstanceId);
        });

        modelBuilder.Entity<UserTaskEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResponseJson).HasColumnType("jsonb");
            entity.HasOne(e => e.WorkflowInstance)
                .WithMany(i => i.UserTasks)
                .HasForeignKey(e => e.WorkflowInstanceId);
        });
    }
}
