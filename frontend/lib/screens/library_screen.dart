import 'package:flutter/material.dart';
import '../models/reading_models.dart';
import '../services/reading_api.dart';
import '../services/session_store.dart';
import '../widgets/mochi_theme.dart';
import 'exam_screen.dart';
import 'result_screen.dart';

class LibraryScreen extends StatefulWidget {
  const LibraryScreen({super.key, required this.api, required this.session});
  final ReadingApi api;
  final SessionStore session;
  @override
  State<LibraryScreen> createState() => _LibraryScreenState();
}

class _LibraryScreenState extends State<LibraryScreen>
    with WidgetsBindingObserver {
  bool loading = true, history = false;
  Object? error;
  List<ExamInfo> exams = [];
  List<AttemptInfo> attempts = [];
  Future<void>? pendingLoad;
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    load(resume: true);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed &&
        (ModalRoute.of(context)?.isCurrent ?? true)) {
      load();
    }
  }

  Future<void> load({bool resume = false}) =>
      pendingLoad ??= loadData(resume: resume).whenComplete(() {
        pendingLoad = null;
      });

  Future<void> loadData({bool resume = false}) async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final data = await Future.wait([
        widget.api.exams(),
        widget.api.attempts(),
      ]);
      if (!mounted) return;
      setState(() {
        exams = data[0] as List<ExamInfo>;
        attempts = data[1] as List<AttemptInfo>;
        loading = false;
      });
      final id = widget.session.activeAttempt;
      if (resume && id != null) {
        final matches = attempts.where((a) => a.id == id);
        if (matches.isNotEmpty) {
          WidgetsBinding.instance.addPostFrameCallback((_) {
            if (mounted) openAttempt(matches.first);
          });
        } else {
          await widget.session.setActive(null);
        }
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          loading = false;
          error = e;
        });
      }
    }
  }

  Future<void> openAttempt(AttemptInfo a) async {
    if (a.finished) {
      await widget.session.setActive(null);
      if (!mounted) return;
      await Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => ResultScreen(result: a, api: widget.api),
        ),
      );
    } else {
      await widget.session.setActive(a.id);
      if (!mounted) return;
      await Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => ExamScreen(
            api: widget.api,
            session: widget.session,
            attemptId: a.id,
          ),
        ),
      );
    }
    if (mounted) await load();
  }

  Future<void> details(ExamInfo exam) async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) =>
            ExamDetails(exam: exam, api: widget.api, session: widget.session),
      ),
    );
    if (mounted) await load();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      leading: const Icon(Icons.auto_stories_rounded, color: Mochi.primary),
      title: const Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'MOCHI TOEIC',
            style: TextStyle(
              color: Mochi.primary,
              fontSize: 10,
              letterSpacing: 1,
            ),
          ),
          Text('Practice Tests'),
        ],
      ),
      actions: [
        IconButton(
          onPressed: () => load(),
          tooltip: 'Tải lại',
          icon: const Icon(Icons.refresh),
        ),
      ],
    ),
    bottomNavigationBar: NavigationBar(
      selectedIndex: history ? 1 : 0,
      onDestinationSelected: (i) => setState(() => history = i == 1),
      destinations: const [
        NavigationDestination(
          icon: Icon(Icons.assignment_outlined),
          label: 'Đề thi',
        ),
        NavigationDestination(icon: Icon(Icons.history), label: 'Lịch sử'),
      ],
    ),
    body: loading
        ? const Center(child: CircularProgressIndicator())
        : error != null
        ? ReadingError(error!, retry: load)
        : MobilePage(
            child: RefreshIndicator(
              onRefresh: load,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  if (!history) ...[
                    Row(
                      children: [
                        Image.asset('assets/mochi.png', width: 52, height: 52),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Chào bạn! ✨',
                                style: TextStyle(fontWeight: FontWeight.w600),
                              ),
                              const Text(
                                'Hôm nay mình cùng luyện Reading nhé!',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: Mochi.muted,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 20),
                    const MochiCard(
                      color: Color(0xffe8f5f2),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Pill('✦ MOCHI READING ENGINE'),
                          SizedBox(height: 12),
                          Text(
                            'Luyện thi TOEIC Reading chuẩn ETS',
                            style: TextStyle(
                              fontSize: 19,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          SizedBox(height: 8),
                          Text(
                            '◷ 75 phút · 100 câu hỏi · Part 5, 6, 7',
                            style: TextStyle(fontSize: 12),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 24),
                    const Text(
                      'Danh sách đề thi',
                      style: TextStyle(
                        fontSize: 19,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 14),
                    if (exams.isEmpty)
                      const MochiCard(
                        child: Column(
                          children: [
                            Icon(
                              Icons.menu_book_outlined,
                              size: 38,
                              color: Mochi.muted,
                            ),
                            SizedBox(height: 12),
                            Text('Chưa có đề được kích hoạt'),
                            SizedBox(height: 8),
                            Text(
                              'Giảng viên cần rà soát và xuất bản đề trước khi bạn bắt đầu.',
                              textAlign: TextAlign.center,
                              style: TextStyle(color: Mochi.muted),
                            ),
                          ],
                        ),
                      ),
                    for (final exam in exams)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: MochiCard(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              Wrap(
                                spacing: 8,
                                runSpacing: 8,
                                children: [
                                  Pill('${exam.sourceId} · v${exam.version}'),
                                  const Pill(
                                    '● Sẵn sàng thi',
                                    color: Mochi.mint,
                                    foreground: Color(0xff3b665b),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 10),
                              Text(
                                exam.title,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w600,
                                  fontSize: 18,
                                ),
                              ),
                              const SizedBox(height: 12),
                              Wrap(
                                spacing: 8,
                                runSpacing: 8,
                                children: [
                                  Pill(
                                    '${exam.count} câu hỏi',
                                    icon: Icons.list_alt,
                                  ),
                                  Pill(
                                    '${exam.minutes} phút',
                                    icon: Icons.schedule,
                                  ),
                                  const Pill(
                                    'Part 5, 6, 7',
                                    color: Color(0xffecfaf5),
                                    foreground: Color(0xff3b665b),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 16),
                              FilledButton(
                                onPressed: () => details(exam),
                                child: const Text('Bắt đầu làm bài →'),
                              ),
                            ],
                          ),
                        ),
                      ),
                  ],
                  if (history || attempts.isNotEmpty) ...[
                    Text(
                      history ? 'Lịch sử luyện tập' : 'Bài làm gần đây',
                      style: const TextStyle(
                        fontSize: 19,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 14),
                    if (attempts.isEmpty)
                      const MochiCard(
                        child: Text('Bạn chưa có lượt làm bài nào.'),
                      ),
                    for (final a in attempts)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: MochiCard(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              Text(
                                a.title,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              const SizedBox(height: 8),
                              Text(
                                a.finished
                                    ? '${a.correct}/${a.total} đúng · ${a.accuracy?.toStringAsFixed(0)}%'
                                    : '${a.answered}/${a.total} đã trả lời',
                                style: const TextStyle(color: Mochi.muted),
                              ),
                              const SizedBox(height: 10),
                              OutlinedButton(
                                onPressed: () => openAttempt(a),
                                child: Text(
                                  a.finished
                                      ? 'Xem kết quả'
                                      : 'Tiếp tục làm bài',
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                  ],
                ],
              ),
            ),
          ),
  );
}

class ExamDetails extends StatefulWidget {
  const ExamDetails({
    super.key,
    required this.exam,
    required this.api,
    required this.session,
  });
  final ExamInfo exam;
  final ReadingApi api;
  final SessionStore session;
  @override
  State<ExamDetails> createState() => _ExamDetailsState();
}

class _ExamDetailsState extends State<ExamDetails> {
  bool busy = false;
  Object? error;
  Future<void> start() async {
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final request = await widget.session.requestId(widget.exam.id);
      final attempt = await widget.api.start(widget.exam.id, request);
      await widget.session.setActive(attempt.id);
      if (!mounted) return;
      if (attempt.finished) {
        await widget.session.clearStart(widget.exam.id);
        if (!mounted) return;
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (_) => ResultScreen(result: attempt, api: widget.api),
          ),
        );
      } else {
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (_) => ExamScreen(
              api: widget.api,
              session: widget.session,
              attemptId: attempt.id,
            ),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          busy = false;
          error = e;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Chi tiết đề thi')),
    body: MobilePage(
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          MochiCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Pill(
                  '✧ Chuẩn ETS Format',
                  color: Mochi.mint,
                  foreground: Color(0xff3b665b),
                ),
                const SizedBox(height: 16),
                Text(
                  widget.exam.title,
                  style: const TextStyle(
                    fontSize: 25,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 12),
                Text(
                  '${widget.exam.sourceId} · Phiên bản ${widget.exam.version}',
                ),
                const SizedBox(height: 20),
                Wrap(
                  spacing: 10,
                  runSpacing: 10,
                  children: [
                    Pill('${widget.exam.minutes} phút', icon: Icons.schedule),
                    Pill(
                      '${widget.exam.count} câu hỏi',
                      icon: Icons.assignment_outlined,
                    ),
                    const Pill('3 phần thi', color: Color(0xfffff1e9)),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
          const Text(
            'Cấu trúc đề thi',
            style: TextStyle(fontSize: 20, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 14),
          for (final row in [
            (5, 'Incomplete Sentences', '30 câu · 101–130'),
            (6, 'Text Completion', '16 câu · 4 nhóm · 131–146'),
            (7, 'Reading Comprehension', '54 câu · 15 nhóm · 147–200'),
          ])
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: MochiCard(
                child: Row(
                  children: [
                    Pill(
                      'P${row.$1}',
                      color: row.$1 == 5 ? Mochi.mint : Mochi.lavender,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            row.$2,
                            style: const TextStyle(fontWeight: FontWeight.w600),
                          ),
                          Text(
                            row.$3,
                            style: const TextStyle(
                              fontSize: 12,
                              color: Mochi.muted,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          const SizedBox(height: 12),
          const MochiCard(
            color: Mochi.lavender,
            child: Text(
              'Thời gian 75 phút được tính liên tục từ lúc bắt đầu. Lựa chọn được lưu sau mỗi thao tác thành công. Tải lại ứng dụng sẽ khôi phục cùng lượt thi và thời hạn. Hết giờ, hệ thống tự thu bài.',
            ),
          ),
          if (error != null)
            Padding(
              padding: const EdgeInsets.all(12),
              child: Text('$error', style: const TextStyle(color: Mochi.wrong)),
            ),
          const SizedBox(height: 24),
          FilledButton.icon(
            onPressed: busy || widget.exam.status != 'PUBLISHED' ? null : start,
            icon: const Icon(Icons.rocket_launch_outlined),
            label: Text(busy ? 'Đang bắt đầu…' : 'Bắt đầu thi ngay'),
          ),
        ],
      ),
    ),
  );
}
