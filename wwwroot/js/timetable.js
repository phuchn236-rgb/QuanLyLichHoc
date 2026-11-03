// Trang Thời khóa biểu: nạp dữ liệu từ Web API và dựng lưới
(function () {
    var $g = $('#tkbGrid'); if (!$g.length) return;
    var endpoint = $g.data('endpoint'), maHK = +$g.data('mahk'), tuan = +$g.data('tuan');

    function load() {
        $('#lblTuan').text(tuan);
        $.getJSON(endpoint, { maHK: maHK, tuan: tuan }, function (r) {
            Tkb.build($g);
            $('#lblRange').text(Tkb.fmt(r.tuanTu) + ' – ' + Tkb.fmt(r.tuanDen));
            Tkb.fill($g, r.items, { hideGv: $g.data('hideGv') == 1 });
            if (!r.items.length) $('#lblRange').append(' · <span class="text-warning">Tuần này chưa có lịch học</span>');
        }).fail(function (x) {
            Tkb.build($g);
            showToast(x.status === 404 ? 'Không tìm thấy dữ liệu thời khóa biểu của tài khoản này.' : 'Không tải được thời khóa biểu.', false);
        });
    }

    $('#btnPrev').on('click', function () { if (tuan > 1) { tuan--; load(); } });
    $('#btnNext').on('click', function () { if (tuan < 20) { tuan++; load(); } });
    $('#selHK').on('change', function () { var u = new URLSearchParams(location.search); u.set('maHK', $(this).val()); u.delete('tuan'); location.search = u.toString(); });
    Tkb.build($g); load();
})();
