using Abp.Zero.EntityFrameworkCore;
using LeHoangMy_1381.MyProject.Authorization.Roles;
using LeHoangMy_1381.MyProject.Authorization.Users;
using LeHoangMy_1381.MyProject.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using LeHoangMy_1381.MyProject.Toeic;
using LeHoangMy_1381.MyProject.EntityFrameworkCore.Toeic;

namespace LeHoangMy_1381.MyProject.EntityFrameworkCore;

public class MyProjectDbContext : AbpZeroDbContext<Tenant, Role, User, MyProjectDbContext>
{
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamPart> ExamParts { get; set; }
    public DbSet<ReadingGroup> ReadingGroups { get; set; }
    public DbSet<ReadingBlock> ReadingBlocks { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<AnswerKey> AnswerKeys { get; set; }
    public DbSet<GapReference> GapReferences { get; set; }
    public DbSet<ExamAttempt> ExamAttempts { get; set; }
    public DbSet<AttemptQuestion> AttemptQuestions { get; set; }
    public DbSet<AttemptOptionOrder> AttemptOptionOrders { get; set; }
    public DbSet<AttemptAnswer> AttemptAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureReading();
    }

    public MyProjectDbContext(DbContextOptions<MyProjectDbContext> options)
        : base(options)
    {
    }
}
