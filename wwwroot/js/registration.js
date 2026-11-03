// Trang Đăng ký học phần: chọn lớp, xem trước trên thời khóa biểu, đăng ký / hủy
(function () {
    var $g = $('#miniGrid'); if (!$g.length) return;
    var ma = $g.data('ma'), maHK = +$g.data('mahk'), tuan = +$g.data('tuan');
    var mine = [], preview = null;

    function inWeek(b) { return b.tuanBatDau <= tuan && tuan <= b.tuanKetThuc; }

    function draw() {
        Tkb.build($g);
        Tkb.fill($g, mine);
        if (preview) Tkb.fill($g, preview.filter(inWeek), { preview: true });
        $('#lblTuan').text(tuan);
    }

    function loadMine() {
        $.getJSON('/api/students/' + encodeURIComponent(ma) + '/timetable', { maHK: maHK, tuan: tuan }, function (r) {
            mine = r.items;
            $('#lblRange').text(Tkb.fmt(r.tuanTu) + ' – ' + Tkb.fmt(r.tuanDen));
            draw();
        }).fail(function () { mine = []; draw(); });
    }

    function info(cls, html) { $('#previewInfo').attr('class', 'alert py-2 small alert-' + cls).html(html); }

    $(document).on('click', '.btn-preview', function () {
        var id = $(this).data('id');
        $('#reg tr').removeClass('table-warning'); $(this).closest('tr').addClass('table-warning');
        $.getJSON('/Registration/Preview', { maLHP: id }, function (r) {
            preview = r.items;
            if (!r.items.length) info('secondary', '<b>' + id + '</b>: lớp học phần này chưa có lịch học.');
            else if (r.registered) info('info', '<b>' + id + '</b>: bạn đã đăng ký lớp này (đang hiển thị trên lưới).');
            else if (r.conflict) info('danger', '<i class="bi bi-x-circle"></i> <b>' + id + '</b>: không thể đăng ký. ' + Tkb.esc(r.conflict));
            else info('success', '<i class="bi bi-check-circle"></i> <b>' + id + '</b>: lịch không trùng, bạn có thể đăng ký. Ô nét đứt màu cam là lịch của lớp này.');
            var first = r.items.filter(function (b) { return b.tuanBatDau <= tuan && tuan <= b.tuanKetThuc; });
            if (r.items.length && !first.length) { tuan = r.items[0].tuanBatDau; loadMine(); } else draw();
        });
    });

    $(document).on('click', '.btn-reg, .btn-unreg', function () {
        var $b = $(this), url = $b.hasClass('btn-reg') ? '/Registration/Register' : '/Registration/Unregister';
        $b.prop('disabled', true);
        $.post(url, { maLHP: $b.data('id') }, function (r) {
            showToast(r.message, r.ok);
            preview = null;
            info('secondary', 'Bấm <b>“Xem lịch”</b> ở một lớp học phần để xem trước lịch học trên thời khóa biểu.');
            if (r.summary) { $('#badgeHP').text(r.summary.soHP); $('#badgeTC').text(r.summary.soTC); }
            AjaxList.reload($('#reg'));   // cập nhật sĩ số và nút Đăng ký / Hủy
            loadMine();                   // thời khóa biểu cá nhân được lưu và hiển thị ngay
        }).fail(function () { showToast('Có lỗi xảy ra, vui lòng thử lại.', false); $b.prop('disabled', false); });
    });

    $('#btnPrev').on('click', function () { if (tuan > 1) { tuan--; loadMine(); } });
    $('#btnNext').on('click', function () { if (tuan < 20) { tuan++; loadMine(); } });
    $(document).on('change', '#filterForm [name="maHK"]', function () { maHK = +$(this).val(); preview = null; loadMine(); });

    Tkb.build($g); loadMine();
})();
