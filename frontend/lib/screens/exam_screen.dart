import 'dart:async';
import 'dart:math';
import 'package:flutter/material.dart';
import '../models/reading_models.dart';
import '../services/reading_api.dart';
import '../services/session_store.dart';
import '../widgets/mochi_theme.dart';
import 'result_screen.dart';

class ExamScreen extends StatefulWidget {
  const ExamScreen({
    super.key,
    required this.api,
    required this.session,
    required this.attemptId,
  });
  final ReadingApi api;
  final SessionStore session;
  final String attemptId;
  @override
  State<ExamScreen> createState() => _ExamScreenState();
}

class _ExamScreenState extends State<ExamScreen> {
  PaperInfo? paper;
  AttemptInfo? state;
  Object? error;
  final answers = <int, AnswerInfo>{};
  final flags = <int>{};
  final elapsed = Stopwatch();
  Duration timeAtSync = Duration.zero;
  Timer? ticker, poller;
  int index = 0;
  bool loading = true,
      saving = false,
      syncing = false,
      submitting = false,
      warningShown = false,
      warningDismissed = false,
      expiredTried = false,
      confirmOpen = false,
      navigated = false;
  String? saveError;
  bool get busy => saving || syncing || submitting;
  Duration get remaining => timeAtSync - elapsed.elapsed;
  bool get locked =>
      state == null || state!.finished || remaining <= Duration.zero;
  int get answered => answers.values.where((a) => a.optionId != null).length;
  String get clock {
    final s = max(0, remaining.inSeconds);
    return '${(s ~/ 3600).toString().padLeft(2, '0')}:${((s % 3600) ~/ 60).toString().padLeft(2, '0')}:${(s % 60).toString().padLeft(2, '0')}';
  }

  @override
  void initState() {
    super.initState();
    load();
  }

  void applyState(AttemptInfo a) {
    state = a;
    answers.clear();
    for (final answer in a.answers) {
      answers[answer.questionId] = answer;
    }
    timeAtSync = a.expires.difference(a.serverNow);
    elapsed
      ..reset()
      ..start();
  }

  Future<void> load() async {
    ticker?.cancel();
    poller?.cancel();
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final data = await Future.wait([
        widget.api.paper(widget.attemptId),
        widget.api.state(widget.attemptId),
      ]);
      if (!mounted) return;
      paper = data[0] as PaperInfo;
      applyState(data[1] as AttemptInfo);
      index =
          (widget.session.preferences.getInt(
                    'reading.index.${widget.attemptId}',
                  ) ??
                  0)
              .clamp(0, paper!.questions.length - 1);
      flags.addAll(
        (widget.session.preferences.getStringList(
                  'reading.flags.${widget.attemptId}',
                ) ??
                [])
            .map(int.parse),
      );
      setState(() => loading = false);
      if (state!.finished) {
        await result(state!);
        return;
      }
      ticker = Timer.periodic(const Duration(seconds: 1), (_) {
        if (!mounted) return;
        setState(() {});
        if (remaining <= Duration.zero && !expiredTried && !busy) {
          expiredTried = true;
          finish(auto: true);
        } else if (remaining.inSeconds <= 300 &&
            remaining > Duration.zero &&
            !warningShown) {
          warningShown = true;
        }
      });
      poller = Timer.periodic(const Duration(seconds: 15), (_) => sync());
    } catch (e) {
      if (mounted) {
        setState(() {
          loading = false;
          error = e;
        });
      }
    }
  }

  Future<void> sync() async {
    if (busy || navigated) return;
    syncing = true;
    try {
      final fresh = await widget.api.state(widget.attemptId);
      if (!mounted) return;
      setState(() => applyState(fresh));
      if (fresh.finished) await result(fresh);
    } catch (e) {
      if (mounted) setState(() => saveError = 'Không thể đồng bộ: $e');
    } finally {
      syncing = false;
    }
  }

  Future<void> choose(QuestionInfo q, int? option) async {
    if (locked || busy) return;
    final old = answers[q.id];
    if (old == null) {
      setState(() => saveError = 'Thiếu trạng thái câu hỏi. Hãy đồng bộ lại.');
      return;
    }
    setState(() {
      saving = true;
      saveError = null;
    });
    try {
      final saved = await widget.api.save(
        widget.attemptId,
        q.id,
        option,
        old.rowVersion,
      );
      if (mounted) setState(() => answers[q.id] = saved);
    } catch (e) {
      if (mounted) setState(() => saveError = 'Chưa lưu lựa chọn. $e');
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  void go(int next) {
    if (busy || paper == null) return;
    setState(() => index = next.clamp(0, paper!.questions.length - 1));
    widget.session.preferences.setInt(
      'reading.index.${widget.attemptId}',
      index,
    );
  }

  void flag(int question) {
    setState(() {
      if (!flags.add(question)) flags.remove(question);
    });
    widget.session.preferences.setStringList(
      'reading.flags.${widget.attemptId}',
      flags.map((n) => '$n').toList(),
    );
  }

  Future<void> confirm() async {
    if (busy || locked) return;
    confirmOpen = true;
    final submit = await showDialog<bool>(
      context: context,
      builder: (dialog) => AlertDialog(
        icon: const Icon(Icons.task_alt, color: Mochi.primary, size: 36),
        title: const Text('Xác nhận nộp bài'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'Bạn có thể nộp bài trước thời hạn. Kết quả sẽ được chấm và lưu bởi hệ thống.',
            ),
            const SizedBox(height: 20),
            Wrap(
              spacing: 12,
              runSpacing: 12,
              children: [
                Pill('Đã trả lời: $answered/100', color: Mochi.mint),
                Pill(
                  'Còn trống: ${100 - answered}',
                  color: const Color(0xffffe8f0),
                  foreground: Mochi.wrong,
                ),
              ],
            ),
            const SizedBox(height: 16),
            Text('Thời gian còn lại: $clock'),
            if (answered < 100)
              const Padding(
                padding: EdgeInsets.only(top: 12),
                child: Text(
                  'Các câu còn trống sẽ được tính là bỏ qua.',
                  style: TextStyle(color: Mochi.warning),
                ),
              ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialog, false),
            child: const Text('Tiếp tục làm bài'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(dialog, true),
            child: const Text('Xác nhận nộp bài'),
          ),
        ],
      ),
    );
    confirmOpen = false;
    if (submit == true && mounted) await finish();
  }

  Future<void> finish({bool auto = false}) async {
    if (busy || navigated) return;
    if (auto && confirmOpen) {
      Navigator.of(context).pop(false);
      confirmOpen = false;
    }
    setState(() {
      submitting = true;
      saveError = null;
    });
    try {
      final submitted = await widget.api.submit(widget.attemptId);
      if (mounted) await result(submitted);
    } catch (e) {
      if (mounted) setState(() => saveError = 'Chưa nhận được kết quả. $e');
    } finally {
      if (mounted) setState(() => submitting = false);
    }
  }

  Future<void> result(AttemptInfo a) async {
    if (navigated) return;
    navigated = true;
    ticker?.cancel();
    poller?.cancel();
    if (confirmOpen && mounted) {
      Navigator.of(context).pop(false);
      confirmOpen = false;
    }
    await widget.session.setActive(null);
    await widget.session.clearStart(a.examId);
    if (!mounted) return;
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(
        builder: (_) => ResultScreen(result: a, api: widget.api),
      ),
    );
  }

  Future<void> palette() async {
    if (paper == null) return;
    var filter = 0;
    final next = await showModalBottomSheet<int>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Mochi.canvas,
      builder: (sheet) => StatefulBuilder(
        builder: (context, setSheet) => SafeArea(
          child: SizedBox(
            height: MediaQuery.sizeOf(context).height * .85,
            child: Column(
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(20, 20, 12, 12),
                  child: Row(
                    children: [
                      const Icon(Icons.grid_view_rounded, color: Mochi.primary),
                      const SizedBox(width: 10),
                      const Expanded(
                        child: Text(
                          'Bảng điều hướng 100 câu',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                      IconButton(
                        onPressed: () => Navigator.pop(sheet),
                        icon: const Icon(Icons.close),
                        tooltip: 'Đóng bảng câu hỏi',
                      ),
                    ],
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 20),
                  child: Column(
                    children: [
                      Wrap(
                        spacing: 10,
                        runSpacing: 8,
                        children: [
                          Pill('Đã làm: $answered/100', color: Mochi.mint),
                          Pill('Chưa làm: ${100 - answered}'),
                        ],
                      ),
                      const SizedBox(height: 12),
                      LinearProgressIndicator(
                        value: answered / 100,
                        color: Mochi.primary,
                        backgroundColor: Mochi.border,
                      ),
                      const SizedBox(height: 12),
                      Wrap(
                        spacing: 8,
                        children: [
                          for (final f in [
                            (0, 'Tất cả'),
                            (1, 'Chưa làm'),
                            (2, 'Đã làm'),
                            (3, 'Đánh dấu'),
                          ])
                            ChoiceChip(
                              label: Text(f.$2),
                              selected: filter == f.$1,
                              onSelected: (_) => setSheet(() => filter = f.$1),
                            ),
                        ],
                      ),
                    ],
                  ),
                ),
                Expanded(
                  child: ListView(
                    padding: const EdgeInsets.all(20),
                    children: [
                      for (final part in [5, 6, 7]) ...[
                        Padding(
                          padding: const EdgeInsets.symmetric(vertical: 12),
                          child: Text(
                            'PART $part · ${part == 5
                                ? 'Incomplete Sentences'
                                : part == 6
                                ? 'Text Completion'
                                : 'Reading Comprehension'}',
                            style: const TextStyle(fontWeight: FontWeight.w600),
                          ),
                        ),
                        Wrap(
                          spacing: 8,
                          runSpacing: 8,
                          children: [
                            for (final q in paper!.questions.where(
                              (q) =>
                                  q.part == part &&
                                  (filter == 0 ||
                                      (filter == 1 &&
                                          answers[q.id]?.optionId == null) ||
                                      (filter == 2 &&
                                          answers[q.id]?.optionId != null) ||
                                      (filter == 3 && flags.contains(q.id))),
                            ))
                              Semantics(
                                label: 'Đi đến câu ${q.number}',
                                button: true,
                                child: InkWell(
                                  onTap: () => Navigator.pop(
                                    sheet,
                                    paper!.questions.indexOf(q),
                                  ),
                                  borderRadius: BorderRadius.circular(12),
                                  child: Container(
                                    width: 48,
                                    height: 48,
                                    alignment: Alignment.center,
                                    decoration: BoxDecoration(
                                      color: answers[q.id]?.optionId != null
                                          ? Mochi.primary
                                          : flags.contains(q.id)
                                          ? const Color(0xfffff6e9)
                                          : Colors.white,
                                      borderRadius: BorderRadius.circular(12),
                                      border: Border.all(
                                        color:
                                            paper!.questions[index].id == q.id
                                            ? Mochi.accent
                                            : Mochi.border,
                                        width:
                                            paper!.questions[index].id == q.id
                                            ? 2
                                            : 1,
                                      ),
                                    ),
                                    child: Text(
                                      '${q.number}${flags.contains(q.id) ? '•' : ''}',
                                      style: TextStyle(
                                        fontSize: 12,
                                        color: answers[q.id]?.optionId != null
                                            ? Colors.white
                                            : Mochi.ink,
                                      ),
                                    ),
                                  ),
                                ),
                              ),
                          ],
                        ),
                      ],
                    ],
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: OutlinedButton.icon(
                    onPressed: () {
                      final i = paper!.questions.indexWhere(
                        (q) => answers[q.id]?.optionId == null,
                      );
                      Navigator.pop(sheet, i < 0 ? index : i);
                    },
                    icon: const Icon(Icons.double_arrow),
                    label: const Text('Chuyển đến câu chưa làm tiếp theo'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
    if (next != null) go(next);
  }

  Widget passage(GroupInfo group) => MochiCard(
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Pill(
          'Nhóm ${group.first}–${group.last} · ${group.blocks.length} tài liệu',
          color: Mochi.mint,
          foreground: const Color(0xff3b665b),
        ),
        const SizedBox(height: 14),
        for (var i = 0; i < group.blocks.length; i++) ...[
          Text(
            'Tài liệu ${i + 1}',
            style: const TextStyle(fontSize: 11, color: Mochi.muted),
          ),
          const SizedBox(height: 8),
          if (group.blocks[i].bytes != null)
            Semantics(
              label:
                  'Bài đọc ${group.first}–${group.last}, tài liệu ${i + 1}. Bấm để phóng to.',
              image: true,
              child: InkWell(
                onTap: () => showDialog<void>(
                  context: context,
                  builder: (zoom) => Dialog.fullscreen(
                    child: Scaffold(
                      appBar: AppBar(
                        title: Text('Tài liệu ${i + 1}'),
                        leading: IconButton(
                          onPressed: () => Navigator.pop(zoom),
                          icon: const Icon(Icons.close),
                        ),
                      ),
                      body: InteractiveViewer(
                        minScale: .5,
                        maxScale: 5,
                        child: Center(
                          child: Image.memory(
                            group.blocks[i].bytes!,
                            fit: BoxFit.contain,
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
                child: Image.memory(
                  group.blocks[i].bytes!,
                  fit: BoxFit.fitWidth,
                  errorBuilder: (_, _, _) => const Text(
                    'Không hiển thị được ảnh bài đọc.',
                    style: TextStyle(color: Mochi.wrong),
                  ),
                ),
              ),
            )
          else
            SelectableText(
              group.blocks[i].text ?? '',
              style: const TextStyle(
                fontFamily: 'Inter',
                fontSize: 16,
                height: 1.8,
              ),
            ),
          const SizedBox(height: 20),
        ],
        const Text(
          'Bấm ảnh để phóng to. Giữ nguyên toàn bộ tài liệu chung khi trả lời câu trong nhóm.',
          style: TextStyle(fontSize: 11, color: Mochi.muted),
        ),
      ],
    ),
  );
  Widget question(QuestionInfo q) {
    final selected = answers[q.id]?.optionId;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        MochiCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      'Question ${q.number}',
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                  IconButton(
                    onPressed: () => flag(q.id),
                    tooltip: flags.contains(q.id) ? 'Bỏ đánh dấu' : 'Đánh dấu',
                    icon: Icon(
                      flags.contains(q.id)
                          ? Icons.bookmark
                          : Icons.bookmark_border,
                      color: flags.contains(q.id) ? Mochi.warning : Mochi.muted,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              if (q.stem.isNotEmpty)
                SelectableText(
                  q.stem,
                  style: const TextStyle(
                    fontFamily: 'Inter',
                    fontSize: 17,
                    height: 1.75,
                  ),
                )
              else
                Text(
                  'Chọn đáp án cho chỗ trống [${q.gap ?? q.number}] trong bài đọc chung.',
                  style: const TextStyle(
                    fontFamily: 'Inter',
                    fontSize: 16,
                    height: 1.7,
                  ),
                ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        for (final option in q.options)
          Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: Semantics(
              label: 'Chọn đáp án ${option.label}',
              selected: selected == option.id,
              button: true,
              enabled: !locked && !busy,
              child: InkWell(
                key: ValueKey('option-${option.label}'),
                onTap: locked || busy ? null : () => choose(q, option.id),
                borderRadius: BorderRadius.circular(14),
                child: Container(
                  padding: const EdgeInsets.all(16),
                  constraints: const BoxConstraints(minHeight: 64),
                  decoration: BoxDecoration(
                    color: selected == option.id
                        ? Mochi.lavender
                        : Colors.white,
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(
                      color: selected == option.id
                          ? Mochi.primary
                          : Mochi.border,
                      width: selected == option.id ? 2 : 1,
                    ),
                  ),
                  child: Row(
                    children: [
                      CircleAvatar(
                        radius: 16,
                        backgroundColor: selected == option.id
                            ? Mochi.primary
                            : Mochi.canvas,
                        child: Text(
                          option.label,
                          style: TextStyle(
                            color: selected == option.id
                                ? Colors.white
                                : Mochi.ink,
                            fontSize: 12,
                          ),
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Text(
                          option.content,
                          style: const TextStyle(
                            fontFamily: 'Inter',
                            fontSize: 16,
                            height: 1.65,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Icon(
                        selected == option.id
                            ? Icons.radio_button_checked
                            : Icons.radio_button_off,
                        size: 20,
                        color: selected == option.id
                            ? Mochi.primary
                            : Mochi.border,
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        Row(
          children: [
            TextButton.icon(
              onPressed: locked || busy || selected == null
                  ? null
                  : () => choose(q, null),
              icon: const Icon(Icons.remove_circle_outline, size: 16),
              label: const Text(
                'Bỏ chọn đáp án',
                style: TextStyle(fontSize: 12),
              ),
            ),
            const Spacer(),
            Flexible(
              child: Text(
                saving
                    ? 'Đang lưu…'
                    : saveError == null
                    ? '☁ Đã đồng bộ'
                    : 'Chưa đồng bộ',
                style: TextStyle(
                  fontSize: 10,
                  color: saveError == null
                      ? const Color(0xff3b665b)
                      : Mochi.wrong,
                ),
              ),
            ),
          ],
        ),
        if (q.groupId != null)
          Wrap(
            spacing: 8,
            children: [
              for (final other in paper!.questions.where(
                (x) => x.groupId == q.groupId,
              ))
                ActionChip(
                  label: Text('${other.number}'),
                  backgroundColor: other.id == q.id ? Mochi.lavender : null,
                  onPressed: busy
                      ? null
                      : () => go(paper!.questions.indexOf(other)),
                ),
            ],
          ),
      ],
    );
  }

  @override
  void dispose() {
    ticker?.cancel();
    poller?.cancel();
    elapsed.stop();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final q = paper == null ? null : paper!.questions[index];
    final group = q?.groupId == null
        ? null
        : paper!.groups.singleWhere((g) => g.id == q!.groupId);
    return PopScope(
      canPop: !busy,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Live Exam Session'),
          actions: [
            IconButton(
              onPressed: busy ? null : sync,
              tooltip: 'Đồng bộ bài làm',
              icon: const Icon(Icons.cloud_sync_outlined),
            ),
            const Padding(
              padding: EdgeInsets.only(right: 16),
              child: CircleAvatar(
                radius: 16,
                backgroundColor: Mochi.primary,
                child: Icon(
                  Icons.person_outline,
                  color: Colors.white,
                  size: 20,
                ),
              ),
            ),
          ],
        ),
        body: loading
            ? const Center(child: CircularProgressIndicator())
            : error != null
            ? ReadingError(error!, retry: load)
            : Column(
                children: [
                  Container(
                    padding: const EdgeInsets.fromLTRB(16, 10, 16, 14),
                    color: const Color(0xfff1f3ff),
                    child: MobilePage(
                      maxWidth: 1400,
                      child: Column(
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: DropdownButtonHideUnderline(
                                  child: DropdownButton<int>(
                                    value: q!.part,
                                    isExpanded: true,
                                    items: const [
                                      DropdownMenuItem(
                                        value: 5,
                                        child: Text(
                                          'Part 5: Incomplete Sentences',
                                          style: TextStyle(fontSize: 11),
                                        ),
                                      ),
                                      DropdownMenuItem(
                                        value: 6,
                                        child: Text(
                                          'Part 6: Text Completion',
                                          style: TextStyle(fontSize: 11),
                                        ),
                                      ),
                                      DropdownMenuItem(
                                        value: 7,
                                        child: Text(
                                          'Part 7: Reading Comprehension',
                                          style: TextStyle(fontSize: 11),
                                        ),
                                      ),
                                    ],
                                    onChanged: busy
                                        ? null
                                        : (part) => go(
                                            paper!.questions.indexWhere(
                                              (x) => x.part == part,
                                            ),
                                          ),
                                  ),
                                ),
                              ),
                              const SizedBox(width: 10),
                              Pill(
                                clock,
                                icon: Icons.schedule,
                                color: remaining.inSeconds <= 300
                                    ? const Color(0xfffff6e9)
                                    : Mochi.lavender,
                                foreground: remaining.inSeconds <= 300
                                    ? Mochi.warning
                                    : Mochi.primary,
                              ),
                            ],
                          ),
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                'Câu ${q.number} · ${index + 1}/100',
                                style: const TextStyle(fontSize: 12),
                              ),
                              Text(
                                '$answered/100 đã trả lời',
                                style: const TextStyle(
                                  fontSize: 12,
                                  color: Mochi.primary,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 8),
                          LinearProgressIndicator(
                            value: answered / 100,
                            backgroundColor: Mochi.border,
                            color: Mochi.primary,
                            minHeight: 5,
                          ),
                        ],
                      ),
                    ),
                  ),
                  if (warningShown && !warningDismissed && !locked)
                    Container(
                      color: const Color(0xfffff6e9),
                      padding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 10,
                      ),
                      child: Row(
                        children: [
                          const Icon(
                            Icons.timer_outlined,
                            color: Mochi.warning,
                          ),
                          const SizedBox(width: 10),
                          const Expanded(
                            child: Text(
                              'Chỉ còn 5 phút thi! Hãy rà soát các câu còn trống.',
                              style: TextStyle(fontSize: 12),
                            ),
                          ),
                          IconButton(
                            onPressed: () {
                              setState(() => warningDismissed = true);
                            },
                            tooltip: 'Ẩn cảnh báo',
                            icon: const Icon(Icons.close, size: 18),
                          ),
                        ],
                      ),
                    ),
                  if (saveError != null)
                    MaterialBanner(
                      content: Text(
                        saveError!,
                        style: const TextStyle(fontSize: 12),
                      ),
                      actions: [
                        TextButton(
                          onPressed: busy
                              ? null
                              : locked
                              ? () => finish(auto: true)
                              : sync,
                          child: Text(locked ? 'Lấy kết quả' : 'Đồng bộ lại'),
                        ),
                      ],
                    ),
                  if (locked)
                    const Padding(
                      padding: EdgeInsets.all(12),
                      child: Text(
                        'Đã hết thời gian. Bài làm đã khóa; hệ thống đang chốt kết quả.',
                        style: TextStyle(color: Mochi.warning),
                      ),
                    ),
                  Expanded(
                    child: LayoutBuilder(
                      builder: (context, box) {
                        if (group != null && box.maxWidth >= 1000) {
                          return Padding(
                            padding: const EdgeInsets.all(24),
                            child: Row(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Expanded(
                                  flex: 55,
                                  child: SingleChildScrollView(
                                    child: passage(group),
                                  ),
                                ),
                                const SizedBox(width: 24),
                                Expanded(
                                  flex: 45,
                                  child: SingleChildScrollView(
                                    child: question(q),
                                  ),
                                ),
                              ],
                            ),
                          );
                        }
                        return MobilePage(
                          maxWidth: 760,
                          child: ListView(
                            key: ValueKey('question-${q.id}'),
                            padding: const EdgeInsets.all(16),
                            children: [
                              if (group != null) ...[
                                passage(group),
                                const SizedBox(height: 18),
                              ],
                              question(q),
                            ],
                          ),
                        );
                      },
                    ),
                  ),
                ],
              ),
        bottomNavigationBar: q == null
            ? null
            : SafeArea(
                child: Container(
                  padding: const EdgeInsets.fromLTRB(12, 8, 12, 12),
                  color: Colors.white,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton(
                              onPressed: index == 0 || busy
                                  ? null
                                  : () => go(index - 1),
                              child: const Text(
                                '‹ Câu trước',
                                style: TextStyle(fontSize: 11),
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          IconButton(
                            onPressed: busy ? null : palette,
                            tooltip: 'Bảng điều hướng 100 câu',
                            icon: const Icon(Icons.grid_view_rounded),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: FilledButton(
                              onPressed: index == 99 || busy
                                  ? null
                                  : () => go(index + 1),
                              child: const Text(
                                'Câu tiếp theo ›',
                                style: TextStyle(fontSize: 11),
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      TextButton.icon(
                        onPressed: busy
                            ? null
                            : locked
                            ? () => finish(auto: true)
                            : confirm,
                        icon: const Icon(Icons.task_alt, size: 17),
                        label: Text(
                          submitting
                              ? 'Đang chấm bài…'
                              : locked
                              ? 'Lấy kết quả'
                              : 'Nộp bài thi',
                        ),
                      ),
                    ],
                  ),
                ),
              ),
      ),
    );
  }
}
